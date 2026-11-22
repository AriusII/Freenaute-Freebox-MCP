using Freenaute.Freebox.Mcp.Core.Services;

namespace Freenaute.Freebox.Mcp.Core.Tests.Services;

public sealed class FreeboxStartupAuthenticationServiceTests
{
    [Fact]
    public async Task MissingCredential_StartsPairingBootstrapEvenWhenToolPairingPolicyIsDisabled()
    {
        var orchestrator = new RecordingOrchestrator
        {
            ConnectionStatus = FreeboxCoreResults.Success(new FreeboxConnectionResult("test", true,
                FreeboxCredentialState.Missing)),
            BeginPairingResult = FreeboxCoreResults.Success(new FreeboxPairingResult(FreeboxCredentialState.Pending)),
            WaitForApprovalResult = FreeboxCoreResults.Success(new FreeboxCredentialResult(FreeboxCredentialState.Approved))
        };
        FreeboxStartupAuthenticationService service = CreateService(orchestrator, allowPairingInitiation: false);

        await service.EnsureAuthenticatedAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, orchestrator.BeginPairingCalls);
        Assert.Equal(1, orchestrator.WaitForApprovalCalls);
    }

    [Fact]
    public async Task ApprovedCredential_WithAuthenticationRequiredProbe_RequestsCredentialRenewal()
    {
        var orchestrator = new RecordingOrchestrator
        {
            ConnectionStatus = FreeboxCoreResults.Success(new FreeboxConnectionResult("test", true,
                FreeboxCredentialState.Approved)),
            SystemSnapshot = FreeboxCoreResults.Failure<FreeboxSystemSnapshot>(
                new FreeboxCoreError(FreeboxCoreErrorCode.AuthenticationRequired, "renewal required")),
            BeginPairingResult = FreeboxCoreResults.Success(new FreeboxPairingResult(FreeboxCredentialState.Pending)),
            WaitForApprovalResult = FreeboxCoreResults.Success(new FreeboxCredentialResult(FreeboxCredentialState.Approved))
        };
        FreeboxStartupAuthenticationService service = CreateService(orchestrator, allowPairingInitiation: false);

        await service.EnsureAuthenticatedAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, orchestrator.GetSystemSnapshotCalls);
        Assert.Equal(1, orchestrator.BeginPairingCalls);
        Assert.Equal(1, orchestrator.WaitForApprovalCalls);
    }

    [Fact]
    public async Task ApprovedCredential_WithSuccessfulProbe_DoesNotStartPairing()
    {
        var orchestrator = new RecordingOrchestrator
        {
            ConnectionStatus = FreeboxCoreResults.Success(new FreeboxConnectionResult("test", true,
                FreeboxCredentialState.Approved)),
            SystemSnapshot = FreeboxCoreResults.Success(new FreeboxSystemSnapshot(
                "1.0",
                "model",
                "board",
                1,
                true,
                true,
                true,
                "ok",
                [],
                [],
                []))
        };
        FreeboxStartupAuthenticationService service = CreateService(orchestrator);

        await service.EnsureAuthenticatedAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, orchestrator.GetSystemSnapshotCalls);
        Assert.Equal(0, orchestrator.BeginPairingCalls);
        Assert.Equal(0, orchestrator.WaitForApprovalCalls);
    }

    [Fact]
    public async Task StartupPairingWait_ApiRejected_RetriesPairingOnce()
    {
        var orchestrator = new RecordingOrchestrator
        {
            ConnectionStatus = FreeboxCoreResults.Success(new FreeboxConnectionResult("test", true,
                FreeboxCredentialState.Missing)),
            BeginPairingResult = FreeboxCoreResults.Success(new FreeboxPairingResult(FreeboxCredentialState.Pending)),
            WaitForApprovalResults =
            [
                FreeboxCoreResults.Failure<FreeboxCredentialResult>(
                    new FreeboxCoreError(FreeboxCoreErrorCode.ApiRejected, "track not found")),
                FreeboxCoreResults.Success(new FreeboxCredentialResult(FreeboxCredentialState.Approved))
            ]
        };
        FreeboxStartupAuthenticationService service = CreateService(orchestrator);

        await service.EnsureAuthenticatedAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, orchestrator.BeginPairingCalls);
        Assert.Equal(2, orchestrator.WaitForApprovalCalls);
    }

    [Fact]
    public async Task PendingCredential_WithStaleTrack_RestartsPairingAndWaitsAgain()
    {
        var orchestrator = new RecordingOrchestrator
        {
            ConnectionStatus = FreeboxCoreResults.Success(new FreeboxConnectionResult("test", true,
                FreeboxCredentialState.Pending)),
            BeginPairingResult = FreeboxCoreResults.Success(new FreeboxPairingResult(FreeboxCredentialState.Pending)),
            WaitForApprovalResults =
            [
                FreeboxCoreResults.Failure<FreeboxCredentialResult>(
                    new FreeboxCoreError(FreeboxCoreErrorCode.ApiRejected, "stale track id")),
                FreeboxCoreResults.Success(new FreeboxCredentialResult(FreeboxCredentialState.Approved))
            ]
        };
        FreeboxStartupAuthenticationService service = CreateService(orchestrator);

        await service.EnsureAuthenticatedAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, orchestrator.ForgetCredentialsCalls);
        Assert.Equal(1, orchestrator.BeginPairingCalls);
        Assert.Equal(2, orchestrator.WaitForApprovalCalls);
    }

    [Fact]
    public async Task StartupWaitTimeout_IsClampedToTwoMinutes()
    {
        var orchestrator = new RecordingOrchestrator
        {
            ConnectionStatus = FreeboxCoreResults.Success(new FreeboxConnectionResult("test", true,
                FreeboxCredentialState.Missing)),
            BeginPairingResult = FreeboxCoreResults.Success(new FreeboxPairingResult(FreeboxCredentialState.Pending)),
            WaitForApprovalResult = FreeboxCoreResults.Success(new FreeboxCredentialResult(FreeboxCredentialState.Approved))
        };
        FreeboxStartupAuthenticationService service = CreateService(orchestrator, pairingWaitTimeout: TimeSpan.FromMinutes(10));

        await service.EnsureAuthenticatedAsync(TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromMinutes(2), orchestrator.LastWaitTimeout);
    }

    private static FreeboxStartupAuthenticationService CreateService(
        IFreeboxOrchestrator orchestrator,
        bool allowPairingInitiation = false,
        TimeSpan? pairingWaitTimeout = null)
    {
        var options = new FreeboxCoreOptions
        {
            AuthenticateOnStartup = true,
            AllowPairingInitiation = allowPairingInitiation,
            PairingWaitTimeout = pairingWaitTimeout ?? TimeSpan.FromMinutes(10)
        };
        return new FreeboxStartupAuthenticationService(
            orchestrator,
            Microsoft.Extensions.Options.Options.Create(options),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<FreeboxStartupAuthenticationService>.Instance);
    }

    private sealed class RecordingOrchestrator : IFreeboxOrchestrator
    {
        public int BeginPairingCalls { get; private set; }
        public int WaitForApprovalCalls { get; private set; }
        public int GetSystemSnapshotCalls { get; private set; }
        public int ForgetCredentialsCalls { get; private set; }
        public TimeSpan? LastWaitTimeout { get; private set; }

        public FreeboxCoreResult<FreeboxConnectionResult> ConnectionStatus { get; init; } =
            FreeboxCoreResults.Success(new FreeboxConnectionResult("test", true, FreeboxCredentialState.Missing));

        public FreeboxCoreResult<FreeboxPairingResult> BeginPairingResult { get; init; } =
            FreeboxCoreResults.Success(new FreeboxPairingResult(FreeboxCredentialState.Pending));

        public FreeboxCoreResult<FreeboxCredentialResult> WaitForApprovalResult { get; init; } =
            FreeboxCoreResults.Success(new FreeboxCredentialResult(FreeboxCredentialState.Approved));

        public IReadOnlyList<FreeboxCoreResult<FreeboxCredentialResult>> WaitForApprovalResults { get; init; } = [];

        public FreeboxCoreResult<FreeboxSystemSnapshot> SystemSnapshot { get; init; } =
            FreeboxCoreResults.Failure<FreeboxSystemSnapshot>(
                new FreeboxCoreError(FreeboxCoreErrorCode.Unknown, "not configured"));

        public ValueTask<FreeboxCoreResult<FreeboxPairingResult>> BeginPairingAsync(
            CancellationToken cancellationToken = default)
        {
            BeginPairingCalls++;
            return ValueTask.FromResult(BeginPairingResult);
        }

        public ValueTask<FreeboxCoreResult<FreeboxCredentialResult>> WaitForApprovalAsync(
            TimeSpan? pollInterval = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            WaitForApprovalCalls++;
            LastWaitTimeout = timeout;
            if (WaitForApprovalResults.Count > 0)
            {
                int index = Math.Min(WaitForApprovalCalls - 1, WaitForApprovalResults.Count - 1);
                return ValueTask.FromResult(WaitForApprovalResults[index]);
            }

            return ValueTask.FromResult(WaitForApprovalResult);
        }

        public ValueTask<FreeboxCoreResult<FreeboxConnectionResult>> GetConnectionStatusAsync(
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(ConnectionStatus);
        }

        public ValueTask<FreeboxCoreResult<bool>> ForgetCredentialsAsync(CancellationToken cancellationToken = default)
        {
            ForgetCredentialsCalls++;
            return ValueTask.FromResult(FreeboxCoreResults.Success(true));
        }

        public ValueTask<FreeboxCoreResult<FreeboxDiscoverySnapshot>> DiscoverAsync(
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(FreeboxCoreResults.Success(new FreeboxDiscoverySnapshot("model", "1", true, 443)));
        }

        public ValueTask<FreeboxCoreResult<FreeboxNetworkSnapshot>> GetNetworkSnapshotAsync(
            bool includeHosts = false,
            string? interfaceName = null,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(FreeboxCoreResults.Failure<FreeboxNetworkSnapshot>(
                new FreeboxCoreError(FreeboxCoreErrorCode.Unknown, "not implemented")));
        }

        public ValueTask<FreeboxCoreResult<FreeboxSystemSnapshot>> GetSystemSnapshotAsync(
            CancellationToken cancellationToken = default)
        {
            GetSystemSnapshotCalls++;
            return ValueTask.FromResult(SystemSnapshot);
        }
    }
}
