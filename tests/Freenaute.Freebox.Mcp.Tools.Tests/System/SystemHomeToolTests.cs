using Freenaute.Freebox.Mapper.Contracts.SystemHome.Standby;
using Freenaute.Freebox.Mapper.Contracts.SystemHome.System;
using Freenaute.Freebox.Mapper.Contracts.SystemHome.Updates;

namespace Freenaute.Freebox.Mcp.Tools.Tests.System;

public sealed class SystemHomeToolTests
{
    [Fact]
    public async Task GetUpdatesStatusAsync_MapsStatesAndSanitizesBoundedVersions()
    {
        UpdateStatus status = new()
        {
            State = new SystemUpdateState("upgrading\u0001"),
            UpgradeState = new UpgradeState
            {
                State = new SystemUpgradeState("rolling\u0002"),
                Percent = 42,
                OldVersion = new string('o', 80),
                NewVersion = "new\u0003version",
                ErrorString = "sensitive diagnostic"
            }
        };
        FreeboxSystemHomeTools tools = CreateTools(getUpdatesAsync: _ => Task.FromResult(status));

        SystemUpdatesStatusToolResult result =
            await tools.GetUpdatesStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal("upgrading", result.State);
        Assert.Equal("rolling", result.UpgradeState);
        Assert.Equal(42, result.UpgradePercent);
        Assert.Equal(64, result.OldVersion.Length);
        Assert.Equal("newversion", result.NewVersion);
    }

    [Fact]
    public async Task GetStandbyStatusAsync_BoundsModesAndSummarizesSchedule()
    {
        StandbyStatus status = new()
        {
            UsePlanning = true,
            PlanningMode = new StandbyPlanningMode("status-mode"),
            NextChange = 1735689600,
            AvailablePlanningModes =
                ImmutableArray.CreateRange(Enumerable.Range(0, 12)
                    .Select(index => new StandbyPlanningMode($"mode-{index}\u0001")))
        };
        StandbyConfig config = new()
        {
            UsePlanning = false,
            PlanningMode = new StandbyPlanningMode("config\u0002mode"),
            Resolution = 300,
            Mapping = ImmutableArray.Create(true, false, true)
        };
        FreeboxSystemHomeTools tools = CreateTools(
            getStandbyStatusAsync: _ => Task.FromResult(status),
            getStandbyConfigAsync: _ => Task.FromResult(config));

        StandbyStatusToolResult result =
            await tools.GetStandbyStatusAsync(TestContext.Current.CancellationToken);

        Assert.False(result.PlanningEnabled);
        Assert.Equal("configmode", result.PlanningMode);
        Assert.Equal(8, result.AvailablePlanningModes.Length);
        Assert.All(result.AvailablePlanningModes, mode => Assert.DoesNotContain('\u0001', mode));
        Assert.Equal(1735689600, result.NextChangeUnixSeconds);
        Assert.Equal(300, result.ResolutionSeconds);
        Assert.Equal(3, result.MappingSlots);
        Assert.Equal(2, result.ActiveMappingSlots);
    }

    [Fact]
    public async Task ConfigureStandbyAsync_UsesStandbyConfigUpdateAndReturnsAggregateConfiguration()
    {
        StandbyConfigUpdate? captured = null;
        StandbyConfig response = new()
        {
            UsePlanning = true,
            PlanningMode = new StandbyPlanningMode("custommode"),
            Resolution = 600,
            Mapping = ImmutableArray.Create(true, false)
        };
        FreeboxSystemHomeTools tools = CreateTools(configureStandbyAsync: (update, _) =>
        {
            captured = update;
            return Task.FromResult(response);
        });

        StandbyConfigurationToolResult result = await tools.ConfigureStandbyAsync(
            true,
            "custommode",
            [true, false],
            TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        StandbyConfigUpdate update = captured;
        Assert.True(update.UsePlanning.IsSet);
        Assert.True(update.UsePlanning.Value);
        Assert.True(update.PlanningMode.IsSet);
        Assert.Equal("custommode", update.PlanningMode.Value.Value);
        Assert.True(update.Mapping.IsSet);
        Assert.Equal([true, false], update.Mapping.Value.ToArray());
        Assert.True(result.PlanningEnabled);
        Assert.Equal("custommode", result.PlanningMode);
        Assert.Equal(2, result.MappingSlots);
        Assert.Equal(1, result.ActiveMappingSlots);
    }

    [Fact]
    public async Task ConfigureStandbyAsync_WhenNoSettingsAreSupplied_RejectsWithoutEndpointCall()
    {
        bool called = false;
        FreeboxSystemHomeTools tools = CreateTools(configureStandbyAsync: (_, _) =>
        {
            called = true;
            return Task.FromResult(new StandbyConfig());
        });

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tools.ConfigureStandbyAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("at least one standby setting", exception.Message, StringComparison.Ordinal);
        Assert.False(called);
    }

    [Theory]
    [InlineData(33)]
    [InlineData(10_081)]
    public async Task ConfigureStandbyAsync_WhenInputExceedsSafetyBound_RejectsWithoutEndpointCall(int length)
    {
        bool called = false;
        FreeboxSystemHomeTools tools = CreateTools(configureStandbyAsync: (_, _) =>
        {
            called = true;
            return Task.FromResult(new StandbyConfig());
        });

        InvalidOperationException exception = length == 33
            ? await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await tools.ConfigureStandbyAsync(
                    planningMode: new string('x', length),
                    cancellationToken: TestContext.Current.CancellationToken))
            : await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await tools.ConfigureStandbyAsync(
                    mapping: new bool[length],
                    cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("InvalidRequest", exception.Message, StringComparison.Ordinal);
        Assert.False(called);
    }

    [Fact]
    public async Task GetUpdatesStatusAsync_WhenEndpointFails_ReturnsMappedFailure()
    {
        FreeboxSystemHomeTools tools =
            CreateTools(getUpdatesAsync: _ => throw new HttpRequestException("offline"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tools.GetUpdatesStatusAsync(TestContext.Current.CancellationToken));

        Assert.Contains("TransportFailure", exception.Message, StringComparison.Ordinal);
    }

    private static FreeboxSystemHomeTools CreateTools(
        Func<CancellationToken, Task<UpdateStatus>>? getUpdatesAsync = null,
        Func<CancellationToken, Task<StandbyStatus>>? getStandbyStatusAsync = null,
        Func<CancellationToken, Task<StandbyConfig>>? getStandbyConfigAsync = null,
        Func<StandbyConfigUpdate, CancellationToken, Task<StandbyConfig>>? configureStandbyAsync = null)
    {
        return new FreeboxSystemHomeTools(
            null,
            getUpdatesAsync ?? (_ => Task.FromResult(new UpdateStatus())),
            getStandbyStatusAsync ?? (_ => Task.FromResult(new StandbyStatus())),
            getStandbyConfigAsync ?? (_ => Task.FromResult(new StandbyConfig())),
            configureStandbyAsync ?? ((_, _) => Task.FromResult(new StandbyConfig())));
    }
}
