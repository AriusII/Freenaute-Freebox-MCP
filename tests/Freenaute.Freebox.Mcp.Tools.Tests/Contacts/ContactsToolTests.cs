namespace Freenaute.Freebox.Mcp.Tools.Tests.Contacts;

public sealed class ContactsToolTests
{
    [Fact]
    public async Task ListAsync_MapsAndBoundsContactSummaries()
    {
        ContactEntry[] contacts =
        [
            new()
            {
                Id = 42,
                FirstName = "Ada",
                LastName = "Lovelace",
                Company = "Analytical Engines",
                LastUpdate = 1234,
                Numbers = [new ContactNumber(), new ContactNumber()],
                Emails = [new ContactEmail()],
                Addresses = [new ContactAddress()],
                Urls = [new ContactUrl(), new ContactUrl(), new ContactUrl()]
            },
            new()
            {
                Id = 43,
                DisplayName = "Grace Hopper"
            }
        ];

        var tools = new FreeboxContactsTools(_ => Task.FromResult(contacts));

        ContactsToolResult result =
            await tools.ListAsync(limit: 1, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(result.Query);
        Assert.Equal(2, result.TotalMatches);
        ContactSummaryToolResult contact = Assert.Single(result.Contacts);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("42"))), contact.Id);
        Assert.Equal("Ada Lovelace", contact.DisplayName);
        Assert.Equal("Analytical Engines", contact.Company);
        Assert.Equal(1234, contact.LastUpdate);
        Assert.Equal(2, contact.NumberCount);
        Assert.Equal(1, contact.EmailCount);
        Assert.Equal(1, contact.AddressCount);
        Assert.Equal(3, contact.UrlCount);
    }

    [Fact]
    public async Task SearchAsync_FiltersByDisplayFirstLastOrCompany()
    {
        ContactEntry[] contacts =
        [
            new() { DisplayName = "Alice", Company = "Alpha" },
            new() { FirstName = "Bob", LastName = "Martin", Company = "Beta" },
            new() { FirstName = "Charlie", LastName = "Delta", Company = "Acme Labs" }
        ];

        var tools = new FreeboxContactsTools(_ => Task.FromResult(contacts));

        ContactsToolResult result =
            await tools.SearchAsync("acme", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("acme", result.Query);
        Assert.Equal(1, result.TotalMatches);
        ContactSummaryToolResult contact = Assert.Single(result.Contacts);
        Assert.Equal("Charlie Delta", contact.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task SearchAsync_BlankQuery_IsRejected(string query)
    {
        var tools = new FreeboxContactsTools(_ => Task.FromResult(Array.Empty<ContactEntry>()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tools.SearchAsync(query, cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("InvalidRequest", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ListAsync_DownstreamFailure_IsMapped()
    {
        var tools = new FreeboxContactsTools(_ => throw new InvalidOperationException("boom"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await tools.ListAsync(cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("InvalidRequest", exception.Message, StringComparison.Ordinal);
    }
}
