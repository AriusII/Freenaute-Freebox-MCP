namespace Freenaute.Freebox.Mcp.Resources.Documentation;

[McpServerResourceType]
public sealed class DocumentationResources
{
    [McpServerResource(
        UriTemplate = "freebox://docs/en/overview",
        Name = "freebox-docs-en-overview",
        Title = "Freebox MCP overview (English)",
        MimeType = "text/markdown")]
    public static TextResourceContents EnglishOverview()
    {
        return Get("freebox://docs/en/overview");
    }

    [McpServerResource(
        UriTemplate = "freebox://docs/en/resources",
        Name = "freebox-docs-en-resources",
        Title = "Freebox resource catalog (English)",
        MimeType = "text/markdown")]
    public static TextResourceContents EnglishResourceCatalog()
    {
        return Get("freebox://docs/en/resources");
    }

    [McpServerResource(
        UriTemplate = "freebox://docs/fr/overview",
        Name = "freebox-docs-fr-overview",
        Title = "Présentation MCP Freebox (français)",
        MimeType = "text/markdown")]
    public static TextResourceContents FrenchOverview()
    {
        return Get("freebox://docs/fr/overview");
    }

    [McpServerResource(
        UriTemplate = "freebox://docs/fr/resources",
        Name = "freebox-docs-fr-resources",
        Title = "Catalogue des ressources Freebox (français)",
        MimeType = "text/markdown")]
    public static TextResourceContents FrenchResourceCatalog()
    {
        return Get("freebox://docs/fr/resources");
    }

    private static TextResourceContents Get(string uri)
    {
        if (!DocumentationStore.TryGet(uri, out string? markdown))
        {
            throw new InvalidOperationException($"Documentation resource '{uri}' was not registered.");
        }

        return new TextResourceContents
        {
            Uri = uri,
            MimeType = "text/markdown",
            Text = markdown
        };
    }
}
