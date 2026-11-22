using Freenaute.Freebox.Mapper.Contracts.Protocol;
using Freenaute.Freebox.Mapper.Contracts.SystemHome.Language;
using Freenaute.Freebox.Mapper.Contracts.SystemHome.Standby;
using Freenaute.Freebox.Mapper.Contracts.SystemHome.System;
using Freenaute.Freebox.Mapper.Contracts.SystemHome.Updates;

namespace Freenaute.Freebox.Mcp.Tools.Tests.Cameras;

public sealed class FreeboxCamerasToolsTests
{
    private static readonly string[] ExpectedStandbyPlanningModes = ["wifi_off", "standby", "custommode"];

    [Fact]
    public async Task GetCamerasAsync_MapsDeterministicallyAndBoundsPayload()
    {
        Camera[] cameras =
        [
            .. Enumerable.Range(0, 40).Select(index => new Camera
            {
                Id = $"camera-{index}",
                NodeId = index,
                Name = $"camera name {index}",
                StreamUrl = index % 2 == 0 ? $"rtsp://cam/{index}" : string.Empty,
                LanId = index % 3 == 0 ? $"lan-{index}" : string.Empty
            })
        ];

        FreeboxCamerasTools tools = CreateTools(_ => Task.FromResult(cameras));

        CamerasToolResult first = await tools.GetCamerasAsync(TestContext.Current.CancellationToken);
        CamerasToolResult second = await tools.GetCamerasAsync(TestContext.Current.CancellationToken);

        Assert.Equal(32, first.Cameras.Length);
        Assert.Equal(first.Cameras.Select(item => item.CameraId), second.Cameras.Select(item => item.CameraId));
        Assert.Equal(Sha256Hex("camera-0"), first.Cameras[0].CameraId);
        Assert.True(first.Cameras[0].StreamAvailable);
        Assert.True(first.Cameras[0].LanBound);
        Assert.False(first.Cameras[1].StreamAvailable);
        Assert.False(first.Cameras[1].LanBound);
    }

    [Fact]
    public async Task GetHomeStatusAsync_MapsDeterministicallyAndSanitizesValues()
    {
        LanguageSupport language = new()
        {
            Lang = "fr\u0001-ca-very-long-language-name",
            AvailableLanguages = ImmutableArray.Create(
                "fr", "en", "it", "es", "de", "nl", "pt", "jp", "sv", "no")
        };
        StandbyStatus standbyStatus = new()
        {
            UsePlanning = true,
            PlanningMode = new StandbyPlanningMode("status-mode"),
            NextChange = 1735689600,
            AvailablePlanningModes = ImmutableArray.Create(
                StandbyPlanningMode.WifiOff,
                StandbyPlanningMode.Standby,
                new StandbyPlanningMode("custom\u0001mode"))
        };
        StandbyConfig standbyConfig = new()
        {
            UsePlanning = false,
            PlanningMode = new StandbyPlanningMode("config\u0001mode"),
            Resolution = 600
        };
        UpdateStatus updates = new()
        {
            State = new SystemUpdateState("upgrading\u0001"),
            UpgradeState = new UpgradeState
            {
                State = new SystemUpgradeState("rolling"),
                Percent = 42
            }
        };

        FreeboxCamerasTools tools = CreateTools(
            getLanguageAsync: _ => Task.FromResult(language),
            getStandbyStatusAsync: _ => Task.FromResult(standbyStatus),
            getStandbyConfigAsync: _ => Task.FromResult(standbyConfig),
            getUpdatesAsync: _ => Task.FromResult(updates));

        HomeStatusToolResult result = await tools.GetHomeStatusAsync(TestContext.Current.CancellationToken);

        Assert.Equal("fr-ca-very-long-", result.CurrentLanguage);
        Assert.Equal(8, result.AvailableLanguages.Length);
        Assert.Equal("configmode", result.StandbyPlanningMode);
        Assert.Equal(ExpectedStandbyPlanningModes, result.AvailableStandbyPlanningModes);
        Assert.False(result.StandbyPlanningEnabled);
        Assert.Equal(1735689600, result.NextStandbyChangeUnixSeconds);
        Assert.Equal(600, result.StandbyResolutionSeconds);
        Assert.Equal("upgrading", result.UpdateState);
        Assert.Equal("rolling", result.UpgradeState);
        Assert.Equal(42, result.UpgradePercent);
    }

    [Fact]
    public async Task GetCamerasAsync_WhenCameraEndpointFails_ThrowsMappedError()
    {
        FreeboxCamerasTools tools = CreateTools(_ => throw new InvalidOperationException("boom"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tools.GetCamerasAsync(TestContext.Current.CancellationToken));

        Assert.Contains("InvalidRequest", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetHomeStatusAsync_WhenHomeEndpointFails_ThrowsMappedError()
    {
        FreeboxCamerasTools tools = CreateTools(getUpdatesAsync: _ => throw new HttpRequestException("offline"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tools.GetHomeStatusAsync(TestContext.Current.CancellationToken));

        Assert.Contains("TransportFailure", exception.Message, StringComparison.Ordinal);
    }

    private static FreeboxCamerasTools CreateTools(
        Func<CancellationToken, Task<Camera[]>>? listCamerasAsync = null,
        Func<CancellationToken, Task<LanguageSupport>>? getLanguageAsync = null,
        Func<CancellationToken, Task<StandbyStatus>>? getStandbyStatusAsync = null,
        Func<CancellationToken, Task<StandbyConfig>>? getStandbyConfigAsync = null,
        Func<CancellationToken, Task<UpdateStatus>>? getUpdatesAsync = null)
    {
        return new FreeboxCamerasTools(
            null,
            listCamerasAsync ?? (_ => Task.FromResult(Array.Empty<Camera>())),
            getLanguageAsync ?? (_ => Task.FromResult(new LanguageSupport())),
            getStandbyStatusAsync ?? (_ => Task.FromResult(new StandbyStatus())),
            getStandbyConfigAsync ?? (_ => Task.FromResult(new StandbyConfig())),
            getUpdatesAsync ?? (_ => Task.FromResult(new UpdateStatus())));
    }

    private static string Sha256Hex(string value)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(hash);
    }
}
