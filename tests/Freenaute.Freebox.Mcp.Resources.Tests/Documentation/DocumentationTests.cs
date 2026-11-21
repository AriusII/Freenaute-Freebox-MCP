namespace Freenaute.Freebox.Mcp.Resources.Tests.Documentation;

public sealed class DocumentationTests
{
    public static TheoryData<string> DocumentUris =>
    [
        "freebox://docs/en/overview",
        "freebox://docs/en/resources",
        "freebox://docs/fr/overview",
        "freebox://docs/fr/resources"
    ];

    [Theory]
    [MemberData(nameof(DocumentUris))]
    public void TryGet_KnownUri_ReturnsEmbeddedMarkdown(string uri)
    {
        bool found = DocumentationStore.TryGet(uri, out string markdown);
        Assert.True(found);
        Assert.NotEmpty(markdown);
        Assert.StartsWith("#", markdown, StringComparison.Ordinal);
        Assert.InRange(markdown.Length, 1, 64 * 1024);
    }

    [Theory]
    [InlineData("FREEBOX://DOCS/EN/OVERVIEW", true)]
    [InlineData("freebox://missing", false)]
    public void TryGet_UsesCaseInsensitiveLookupAndRejectsUnknownUris(string uri, bool expected)
    {
        bool found = DocumentationStore.TryGet(uri, out string markdown);
        Assert.Equal(expected, found);
        Assert.Equal(expected, !string.IsNullOrEmpty(markdown));
    }

    [Fact]
    public void TryGet_NullUri_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DocumentationStore.TryGet(null!, out _));
    }

    [Theory]
    [InlineData(nameof(DocumentationResources.EnglishOverview), "freebox://docs/en/overview",
        "freebox-docs-en-overview", "Freebox MCP overview (English)")]
    [InlineData(nameof(DocumentationResources.EnglishResourceCatalog), "freebox://docs/en/resources",
        "freebox-docs-en-resources", "Freebox resource catalog (English)")]
    [InlineData(nameof(DocumentationResources.FrenchOverview), "freebox://docs/fr/overview", "freebox-docs-fr-overview",
        "Présentation MCP Freebox (français)")]
    [InlineData(nameof(DocumentationResources.FrenchResourceCatalog), "freebox://docs/fr/resources",
        "freebox-docs-fr-resources", "Catalogue des ressources Freebox (français)")]
    public void DocumentationMethod_ReturnsContentMatchingMetadata(string methodName, string uri, string name,
        string title)
    {
        MethodInfo method = typeof(DocumentationResources).GetMethod(methodName)!;
        McpServerResourceAttribute metadata = Assert.Single(method.GetCustomAttributes<McpServerResourceAttribute>());
        Assert.Equal(uri, metadata.UriTemplate);
        Assert.Equal(name, metadata.Name);
        Assert.Equal(title, metadata.Title);
        Assert.Equal("text/markdown", metadata.MimeType);

        TextResourceContents content = Assert.IsType<TextResourceContents>(method.Invoke(null, null));
        Assert.Equal(uri, content.Uri);
        Assert.Equal("text/markdown", content.MimeType);
        Assert.NotEmpty(content.Text);
    }
}
