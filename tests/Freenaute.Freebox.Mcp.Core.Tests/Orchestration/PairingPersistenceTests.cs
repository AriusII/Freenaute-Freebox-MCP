using System.Diagnostics.CodeAnalysis;

namespace Freenaute.Freebox.Mcp.Core.Tests.Orchestration;

public sealed class PairingPersistenceTests
{
    [Fact]
    public async Task ApprovedCredentials_ArePersistedAndReusedByCoordinator()
    {
        var id = new FreeboxCredentialId("profile", "application", "freebox-uid");
        var storage = new RecordingCredentialStorage();
        var coordinator = new StatefulAuthorizationCoordinator(storage, id);
        var credentials = new FreeboxAppCredentials(
            "application",
            "secret-token",
            FreeboxCredentialStatus.Approved,
            42,
            DateTimeOffset.Parse("2026-01-02T03:04:05Z", CultureInfo.InvariantCulture));
        storage.Credentials[id] = credentials;

        FreeboxOrchestrator orchestrator = CreateOrchestrator(coordinator, new FreeboxCredentialStoreAdapter(storage));
        FreeboxCoreResult<FreeboxCredentialResult> result = await orchestrator.WaitForApprovalAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(FreeboxCredentialState.Approved, result.Value!.State);
        Assert.Equal(credentials.CreatedAt, result.Value.CreatedAt);
        Assert.Equal(credentials.UpdatedAt, result.Value.UpdatedAt);
        Assert.Equal(id, Assert.Single(storage.ReadIds));
    }

    [Fact]
    public async Task ForgetCredentials_RemovesPersistedCredentialAndSubsequentReuseReturnsNone()
    {
        var id = new FreeboxCredentialId("profile", "application", "freebox-uid");
        var storage = new RecordingCredentialStorage();
        var coordinator = new StatefulAuthorizationCoordinator(storage, id);
        storage.Credentials[id] = new FreeboxAppCredentials(
            "application", "secret-token", FreeboxCredentialStatus.Approved, 42, DateTimeOffset.UtcNow);
        FreeboxOrchestrator orchestrator = CreateOrchestrator(coordinator, new FreeboxCredentialStoreAdapter(storage));

        FreeboxCoreResult<bool> result = await orchestrator.ForgetCredentialsAsync(
            TestContext.Current.CancellationToken);
        FreeboxCoreResult<FreeboxCredentialResult> reused = await orchestrator.WaitForApprovalAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
        Assert.Equal(id, Assert.Single(storage.DeletedIds));
        Assert.False(storage.Credentials.ContainsKey(id));
        Assert.False(reused.IsSuccess);
    }

    [Fact]
    public async Task Status_UnavailableStoreFailsClosedWithoutReadingToken()
    {
        var storage = new RecordingCredentialStorage { IsAvailable = false, Kind = "secure-test-store" };
        FreeboxOrchestrator orchestrator = CreateOrchestrator(new RecordingAuthorizationCoordinator(),
            new FreeboxCredentialStoreAdapter(storage));

        FreeboxCoreResult<FreeboxConnectionResult> result = await orchestrator.GetConnectionStatusAsync(
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CredentialStoreAvailable);
        Assert.Equal("secure-test-store", result.Value.CredentialStoreKind);
        Assert.Equal(FreeboxCredentialState.Unavailable, result.Value.CredentialState);
        Assert.Empty(storage.ReadIds);
    }

    [Theory]
    [InlineData(FreeboxCredentialStatus.Pending, FreeboxCredentialState.Pending)]
    [InlineData(FreeboxCredentialStatus.Approved, FreeboxCredentialState.Approved)]
    [InlineData(FreeboxCredentialStatus.Invalid, FreeboxCredentialState.Corrupt)]
    public async Task Status_ReadsDurableCredentialUsingDiscoveredFreeboxUid(
        FreeboxCredentialStatus storedState,
        FreeboxCredentialState expectedState)
    {
        var storage = new RecordingCredentialStorage { IsAvailable = true };
        var id = new FreeboxCredentialId("default", "org.freenaute.freebox-mcp", "discovered-uid");
        storage.Credentials[id] = new FreeboxAppCredentials(
            id.ApplicationId,
            "durable-secret",
            storedState,
            7,
            DateTimeOffset.UtcNow);
        FreeboxOrchestrator orchestrator = CreateOrchestrator(
            new RecordingAuthorizationCoordinator(),
            new FreeboxCredentialStoreAdapter(storage),
            client: CreateClient("discovered-uid"));

        FreeboxCoreResult<FreeboxConnectionResult> result = await orchestrator.GetConnectionStatusAsync(
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedState, result.Value!.CredentialState);
        Assert.Equal(id, Assert.Single(storage.ReadIds));
        Assert.DoesNotContain("durable-secret", JsonSerializer.Serialize(result), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Status_ReportsMissingForDiscoveredIdentityWithoutCredential()
    {
        var storage = new RecordingCredentialStorage { IsAvailable = true };
        FreeboxOrchestrator orchestrator = CreateOrchestrator(
            new RecordingAuthorizationCoordinator(),
            new FreeboxCredentialStoreAdapter(storage),
            client: CreateClient("discovered-uid"));

        FreeboxCoreResult<FreeboxConnectionResult> result = await orchestrator.GetConnectionStatusAsync(
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(FreeboxCredentialState.Missing, result.Value!.CredentialState);
    }

    [Fact]
    public async Task Status_ReportsCorruptWithoutExposingStoreException()
    {
        var storage = new RecordingCredentialStorage { IsAvailable = true, ThrowInvalidData = true };
        FreeboxOrchestrator orchestrator = CreateOrchestrator(
            new RecordingAuthorizationCoordinator(),
            new FreeboxCredentialStoreAdapter(storage),
            client: CreateClient("discovered-uid"));

        FreeboxCoreResult<FreeboxConnectionResult> result = await orchestrator.GetConnectionStatusAsync(
            TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(FreeboxCredentialState.Corrupt, result.Value!.CredentialState);
    }

    [Fact]
    public async Task PairingResult_AndCredentialResult_DoNotExposeAppToken()
    {
        const string token = "must-not-escape";
        var storage = new RecordingCredentialStorage();
        var coordinator = new RecordingAuthorizationCoordinator
        {
            Registration = new FreeboxApprovalRegistration(FreeboxCredentialStatus.Pending, 123),
            Credentials = new FreeboxAppCredentials("application", token, FreeboxCredentialStatus.Approved, 123,
                DateTimeOffset.UtcNow)
        };
        FreeboxOrchestrator orchestrator = CreateOrchestrator(coordinator,
            new FreeboxCredentialStoreAdapter(storage), true);

        FreeboxCoreResult<FreeboxPairingResult> pairing = await orchestrator.BeginPairingAsync(
            TestContext.Current.CancellationToken);
        FreeboxCoreResult<FreeboxCredentialResult> approval = await orchestrator.WaitForApprovalAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        string serialized = JsonSerializer.Serialize(new { pairing, approval });

        Assert.True(pairing.IsSuccess);
        Assert.True(approval.IsSuccess);
        Assert.DoesNotContain(token, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("AppToken", serialized, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(AuthorizationTrackEnum.Denied, FreeboxCredentialState.Denied)]
    [InlineData(AuthorizationTrackEnum.Timeout, FreeboxCredentialState.Expired)]
    public async Task ApprovalTerminalFailures_ReturnTypedStateWithoutCredentials(
        AuthorizationTrackEnum authorizationState,
        FreeboxCredentialState expectedState)
    {
        var coordinator = new RecordingAuthorizationCoordinator { AuthorizationFailure = authorizationState };
        FreeboxOrchestrator orchestrator = CreateOrchestrator(coordinator, new RecordingCredentialStore());

        FreeboxCoreResult<FreeboxCredentialResult> result = await orchestrator.WaitForApprovalAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedState, result.Value!.State);
        Assert.Null(result.Value.CreatedAt);
        Assert.Null(result.Value.UpdatedAt);
    }

    [Fact]
    public async Task CredentialStoreAdapter_VerifiesWriteAndDeleteAgainstDurableStore()
    {
        var storage = new RecordingCredentialStorage { IsAvailable = true };
        var adapter = new FreeboxCredentialStoreAdapter(storage);
        var id = new FreeboxCredentialId("profile", "application", "uid");
        var credentials = new FreeboxAppCredentials(
            "application", "secret", FreeboxCredentialStatus.Approved, 3, DateTimeOffset.UtcNow);

        await adapter.PutAsync(id, credentials, TestContext.Current.CancellationToken);
        FreeboxAppCredentials? stored = await adapter.GetAsync(id, TestContext.Current.CancellationToken);
        await adapter.DeleteAsync(id, TestContext.Current.CancellationToken);

        Assert.NotNull(stored);
        Assert.False(storage.Credentials.ContainsKey(id));
        Assert.Equal(3, storage.ReadIds.Count);
        Assert.Equal(id, Assert.Single(storage.DeletedIds));
    }

    [Fact]
    public async Task CredentialStoreAdapter_RejectsWriteThatCannotBeReadBack()
    {
        var storage = new RecordingCredentialStorage { IsAvailable = true, SuppressPut = true };
        var adapter = new FreeboxCredentialStoreAdapter(storage);
        var id = new FreeboxCredentialId("profile", "application", "uid");
        var credentials = new FreeboxAppCredentials(
            "application", "secret", FreeboxCredentialStatus.Approved, 3, DateTimeOffset.UtcNow);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await adapter.PutAsync(id, credentials, TestContext.Current.CancellationToken));

        Assert.DoesNotContain("secret", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task PairingPolicy_OnlyPermitsExplicitTrueOptionsValue()
    {
        var coordinator = new RecordingAuthorizationCoordinator
        {
            Registration = new FreeboxApprovalRegistration(FreeboxCredentialStatus.Pending, 1)
        };
        FreeboxOrchestrator denied = CreateOrchestrator(coordinator, new RecordingCredentialStore());

        FreeboxCoreResult<FreeboxPairingResult> deniedResult = await denied.BeginPairingAsync(
            TestContext.Current.CancellationToken);
        Assert.False(deniedResult.IsSuccess);
        Assert.Equal(0, coordinator.BeginCalls);

        FreeboxOrchestrator allowed = CreateOrchestrator(coordinator, new RecordingCredentialStore(), true);
        FreeboxCoreResult<FreeboxPairingResult> allowedResult = await allowed.BeginPairingAsync(
            TestContext.Current.CancellationToken);
        Assert.True(allowedResult.IsSuccess);
        Assert.Equal(1, coordinator.BeginCalls);
    }

    [Fact]
    public async Task Pairing_FailsClosedWhenSecureStoreIsUnavailable()
    {
        var coordinator = new RecordingAuthorizationCoordinator();
        var store = new RecordingCredentialStore { IsAvailableOverride = false };
        FreeboxOrchestrator orchestrator = CreateOrchestrator(coordinator, store, true);

        FreeboxCoreResult<FreeboxPairingResult> begin = await orchestrator.BeginPairingAsync(
            TestContext.Current.CancellationToken);
        FreeboxCoreResult<FreeboxCredentialResult> wait = await orchestrator.WaitForApprovalAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(begin.IsSuccess);
        Assert.Equal(FreeboxCoreErrorCode.CredentialStoreUnavailable, begin.Error!.Code);
        Assert.False(wait.IsSuccess);
        Assert.Equal(FreeboxCoreErrorCode.CredentialStoreUnavailable, wait.Error!.Code);
        Assert.Equal(0, coordinator.BeginCalls);
        Assert.Equal(0, coordinator.WaitCalls);
    }

    [Fact]
    public async Task PreCancelledOperations_PropagateCancellationWithoutCallingDependencies()
    {
        var coordinator = new RecordingAuthorizationCoordinator();
        var storage = new RecordingCredentialStorage();
        FreeboxOrchestrator orchestrator = CreateOrchestrator(coordinator,
            new FreeboxCredentialStoreAdapter(storage), true);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await orchestrator.BeginPairingAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await orchestrator.GetConnectionStatusAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await orchestrator.ForgetCredentialsAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await orchestrator.WaitForApprovalAsync(cancellationToken: cancellation.Token));

        Assert.Equal(0, coordinator.BeginCalls);
        Assert.Equal(0, coordinator.ForgetCalls);
        Assert.Equal(0, coordinator.WaitCalls);
        Assert.Empty(storage.ReadIds);
    }

    [Fact]
    public async Task MidFlightCancellation_IsNotConvertedIntoFailure()
    {
        var coordinator = new RecordingAuthorizationCoordinator { ThrowCancellationOnWait = true };
        FreeboxOrchestrator orchestrator = CreateOrchestrator(coordinator, new RecordingCredentialStore());
        using var cancellation = new CancellationTokenSource();
        Task<FreeboxCoreResult<FreeboxCredentialResult>> waiting = orchestrator.WaitForApprovalAsync(
            cancellationToken: cancellation.Token).AsTask();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await waiting);
    }

    [Fact]
    public async Task CancelledApprovalWait_CanBeResumedWithoutBeginningPairingAgain()
    {
        var coordinator = new RecordingAuthorizationCoordinator { ThrowCancellationOnWait = true };
        FreeboxOrchestrator orchestrator = CreateOrchestrator(coordinator, new RecordingCredentialStore());
        using var cancellation = new CancellationTokenSource();
        Task<FreeboxCoreResult<FreeboxCredentialResult>> waiting = orchestrator.WaitForApprovalAsync(
            cancellationToken: cancellation.Token).AsTask();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await waiting);

        FreeboxCoreResult<FreeboxCredentialResult> resumed = await orchestrator.WaitForApprovalAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(resumed.IsSuccess);
        Assert.Equal(FreeboxCredentialState.Approved, resumed.Value!.State);
        Assert.Equal(0, coordinator.BeginCalls);
        Assert.Equal(2, coordinator.WaitCalls);
    }

    private static FreeboxOrchestrator CreateOrchestrator(
        IFreeboxAuthorizationCoordinator authorization,
        IFreeboxCredentialStore credentialStore,
        bool allowPairing = false,
        IFreeboxClient? client = null)
    {
        client ??= DispatchProxy.Create<IFreeboxClient, ThrowingClientProxy>()!;
        return new FreeboxOrchestrator(client, authorization, credentialStore,
            Microsoft.Extensions.Options.Options.Create(new FreeboxCoreOptions
            {
                ApplicationId = "org.freenaute.freebox-mcp",
                CredentialProfile = "default",
                AllowPairingInitiation = allowPairing
            }));
    }

    private static IFreeboxClient CreateClient(string uid)
    {
        IFreeboxClient proxy = DispatchProxy.Create<IFreeboxClient, DiscoveryClientProxy>()!;
        ((DiscoveryClientProxy)proxy).ApiVersion = new ApiVersionResponse(
            uid, "Freebox", "Freebox", "Freebox", "v15", "mafreebox.freebox.fr", "api", true, 443);
        return proxy;
    }

    private sealed class RecordingCredentialStorage : IFreeboxCredentialStorage
    {
        public Dictionary<FreeboxCredentialId, FreeboxAppCredentials> Credentials { get; } = [];
        public List<FreeboxCredentialId> ReadIds { get; } = [];
        public List<FreeboxCredentialId> DeletedIds { get; } = [];
        public bool ThrowInvalidData { get; init; }
        public bool SuppressPut { get; init; }
        public bool IsAvailable { get; set; } = true;
        public string Kind { get; set; } = "recording";

        public ValueTask<FreeboxAppCredentials?> GetAsync(FreeboxCredentialId id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadIds.Add(id);
            if (ThrowInvalidData)
            {
                throw new InvalidDataException("redacted test corruption detail");
            }

            Credentials.TryGetValue(id, out FreeboxAppCredentials? value);
            return ValueTask.FromResult(value);
        }

        public ValueTask PutAsync(FreeboxCredentialId id, FreeboxAppCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!SuppressPut)
            {
                Credentials[id] = credentials;
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask DeleteAsync(FreeboxCredentialId id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeletedIds.Add(id);
            Credentials.Remove(id);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingCredentialStore : IFreeboxCredentialStore
    {
        public bool IsAvailableOverride { get; init; } = true;
        public bool IsAvailable => IsAvailableOverride;
        public string Kind => "recording";

        public ValueTask<FreeboxAppCredentials?> GetAsync(FreeboxCredentialId id,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<FreeboxAppCredentials?>(null);
        }

        public ValueTask PutAsync(FreeboxCredentialId id, FreeboxAppCredentials credentials,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DeleteAsync(FreeboxCredentialId id, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class StatefulAuthorizationCoordinator(
        RecordingCredentialStorage storage,
        FreeboxCredentialId id) : IFreeboxAuthorizationCoordinator
    {
        public ValueTask<FreeboxApprovalRegistration> BeginPairingAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new FreeboxApprovalRegistration(FreeboxCredentialStatus.Pending, 42));
        }

        public async ValueTask<FreeboxAppCredentials> WaitForApprovalAsync(TimeSpan? pollInterval = null,
            CancellationToken cancellationToken = default)
        {
            FreeboxAppCredentials? credentials = await storage.GetAsync(id, cancellationToken);
            return credentials ?? throw new InvalidOperationException("No persisted credential.");
        }

        public ValueTask ForgetAsync(CancellationToken cancellationToken = default)
        {
            return storage.DeleteAsync(id, cancellationToken);
        }
    }

    private sealed class RecordingAuthorizationCoordinator : IFreeboxAuthorizationCoordinator
    {
        public int BeginCalls { get; private set; }
        public int ForgetCalls { get; private set; }
        public int WaitCalls { get; private set; }
        public bool ThrowCancellationOnWait { get; init; }
        public AuthorizationTrackEnum? AuthorizationFailure { get; init; }

        public FreeboxApprovalRegistration Registration { get; init; } =
            new(FreeboxCredentialStatus.Pending, 1);

        public FreeboxAppCredentials Credentials { get; init; } =
            new("application", "token", FreeboxCredentialStatus.Approved, 1, DateTimeOffset.UtcNow);

        public ValueTask<FreeboxApprovalRegistration> BeginPairingAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BeginCalls++;
            return ValueTask.FromResult(Registration);
        }

        public async ValueTask<FreeboxAppCredentials> WaitForApprovalAsync(TimeSpan? pollInterval = null,
            CancellationToken cancellationToken = default)
        {
            WaitCalls++;
            if (ThrowCancellationOnWait && WaitCalls == 1)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (AuthorizationFailure is { } failure)
            {
                throw new FreeboxAuthorizationException(failure);
            }

            return Credentials;
        }

        public ValueTask ForgetAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ForgetCalls++;
            return ValueTask.CompletedTask;
        }
    }

    [SuppressMessage(
        "Performance", "CA1852:Seal internal types",
        Justification = "DispatchProxy derives a runtime-generated proxy from this base type.")]
    private class DiscoveryClientProxy : DispatchProxy
    {
        public ApiVersionResponse ApiVersion { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Discovery")
            {
                return Create<IFreeboxDiscoveryApi, DiscoveryApiProxy>()!
                    is IFreeboxDiscoveryApi discovery
                    ? SetApiVersion(discovery)
                    : null;
            }

            throw new InvalidOperationException("Only discovery is available in credential status tests.");
        }

        private IFreeboxDiscoveryApi SetApiVersion(IFreeboxDiscoveryApi discovery)
        {
            ((DiscoveryApiProxy)discovery).ApiVersion = ApiVersion;
            return discovery;
        }
    }

    [SuppressMessage(
        "Performance", "CA1852:Seal internal types",
        Justification = "DispatchProxy derives a runtime-generated proxy from this base type.")]
    private class DiscoveryApiProxy : DispatchProxy
    {
        public ApiVersionResponse ApiVersion { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name == nameof(IFreeboxDiscoveryApi.GetApiVersionAsync)
                ? Task.FromResult(ApiVersion)
                : throw new InvalidOperationException("Only API version discovery is available.");
        }
    }

    [SuppressMessage(
        "Performance", "CA1852:Seal internal types",
        Justification = "DispatchProxy derives a runtime-generated proxy from this base type.")]
    private class ThrowingClientProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            throw new InvalidOperationException("Client must not be used by pairing coordination tests.");
        }
    }
}
