using Freenaute.Freebox.Mapper.Common.Types;

namespace Freenaute.Freebox.Mcp.Tools.Tests.AirMedia;

public sealed class AirMediaToolTests
{
    [Fact]
    public async Task GetStatusAsync_MapsConfigurationAndSanitizedReceivers()
    {
        var api = new RecordingAirMediaApi();
        var tools = new FreeboxAirMediaTools(api);

        AirMediaStatusToolResult result = await tools.GetStatusAsync(
            TestContext.Current.CancellationToken);

        Assert.True(result.Enabled);
        Assert.Single(result.Receivers);
        Assert.Equal("Living Room", result.Receivers[0].Name);
        Assert.True(result.Receivers[0].PasswordProtected);
        Assert.True(result.Receivers[0].CanDisplayPhotos);
        Assert.True(result.Receivers[0].CanPlayAudio);
        Assert.True(result.Receivers[0].CanPlayVideo);
        Assert.False(result.Receivers[0].CanDisplayScreen);
    }

    [Fact]
    public async Task GetReceiversAsync_Failure_IsMappedToInvalidOperationException()
    {
        var api = new RecordingAirMediaApi
        {
            ReceiversException = new InvalidOperationException("receiver lookup failed")
        };
        var tools = new FreeboxAirMediaTools(api);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                tools.GetReceiversAsync(TestContext.Current.CancellationToken));

        Assert.Contains("InvalidRequest", exception.Message, StringComparison.Ordinal);
        Assert.Contains("receiver lookup failed", exception.Message, StringComparison.Ordinal);
    }

    private sealed class RecordingAirMediaApi : IFreeboxAirMediaApi
    {
        public Exception? ReceiversException { get; init; }

        public Task<AirMediaConfigResponse> GetConfigurationAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AirMediaConfigResponse(true));
        }

        public Task<AirMediaConfigResponse> UpdateConfigurationAsync(
            UpdateAirMediaConfigRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<AirMediaReceiverResponse>> GetReceiversAsync(
            CancellationToken cancellationToken = default)
        {
            if (ReceiversException is not null)
            {
                return Task.FromException<IReadOnlyList<AirMediaReceiverResponse>>(ReceiversException);
            }

            IReadOnlyList<AirMediaReceiverResponse> receivers =
            [
                new(
                    "Living\u0000 Room",
                    true,
                    new AirMediaCapabilities(
                        true, true, true, false))
            ];
            return Task.FromResult(receivers);
        }

        public Task SendToReceiverAsync(
            string receiverName,
            AirMediaReceiverRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public FreeboxAirMediaReceiver Receiver(string receiverName)
        {
            throw new NotSupportedException();
        }
    }
}
