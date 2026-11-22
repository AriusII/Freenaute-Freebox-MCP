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
        UriTemplate = "freebox://docs/en/quickstart",
        Name = "freebox-docs-en-quickstart",
        Title = "Freebox MCP quickstart (English)",
        MimeType = "text/markdown")]
    public static TextResourceContents EnglishQuickstart()
    {
        return Get("freebox://docs/en/quickstart");
    }

    [McpServerResource(
        UriTemplate = "freebox://docs/en/auth-pairing",
        Name = "freebox-docs-en-auth-pairing",
        Title = "Freebox authentication and pairing (English)",
        MimeType = "text/markdown")]
    public static TextResourceContents EnglishAuthPairing()
    {
        return Get("freebox://docs/en/auth-pairing");
    }

    [McpServerResource(
        UriTemplate = "freebox://docs/en/tool-families",
        Name = "freebox-docs-en-tool-families",
        Title = "Freebox MCP tool-family guide (English)",
        MimeType = "text/markdown")]
    public static TextResourceContents EnglishToolFamilies()
    {
        return Get("freebox://docs/en/tool-families");
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

    [McpServerResource(
        UriTemplate = "freebox://docs/fr/quickstart",
        Name = "freebox-docs-fr-quickstart",
        Title = "Démarrage rapide MCP Freebox (français)",
        MimeType = "text/markdown")]
    public static TextResourceContents FrenchQuickstart()
    {
        return Get("freebox://docs/fr/quickstart");
    }

    [McpServerResource(
        UriTemplate = "freebox://docs/fr/authentification-appairage",
        Name = "freebox-docs-fr-authentification-appairage",
        Title = "Authentification et appairage Freebox (français)",
        MimeType = "text/markdown")]
    public static TextResourceContents FrenchAuthPairing()
    {
        return Get("freebox://docs/fr/authentification-appairage");
    }

    [McpServerResource(
        UriTemplate = "freebox://docs/fr/tool-families",
        Name = "freebox-docs-fr-tool-families",
        Title = "Guide des familles d'outils MCP Freebox (français)",
        MimeType = "text/markdown")]
    public static TextResourceContents FrenchToolFamilies()
    {
        return Get("freebox://docs/fr/tool-families");
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
