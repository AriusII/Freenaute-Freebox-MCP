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

    [McpServerResource(UriTemplate = "freebox://docs/en/domain-network-advanced", Name = "freebox-docs-en-domain-network-advanced", Title = "Advanced network domain guide (English)", MimeType = "text/markdown")]
    public static TextResourceContents EnglishDomainNetworkAdvanced() => Get("freebox://docs/en/domain-network-advanced");

    [McpServerResource(UriTemplate = "freebox://docs/en/domain-files-advanced", Name = "freebox-docs-en-domain-files-advanced", Title = "Advanced files domain guide (English)", MimeType = "text/markdown")]
    public static TextResourceContents EnglishDomainFilesAdvanced() => Get("freebox://docs/en/domain-files-advanced");

    [McpServerResource(UriTemplate = "freebox://docs/en/domain-systemhome-controls", Name = "freebox-docs-en-domain-systemhome-controls", Title = "System and home controls guide (English)", MimeType = "text/markdown")]
    public static TextResourceContents EnglishDomainSystemHomeControls() => Get("freebox://docs/en/domain-systemhome-controls");

    [McpServerResource(UriTemplate = "freebox://docs/en/domain-services-controls", Name = "freebox-docs-en-domain-services-controls", Title = "Services controls guide (English)", MimeType = "text/markdown")]
    public static TextResourceContents EnglishDomainServicesControls() => Get("freebox://docs/en/domain-services-controls");

    [McpServerResource(UriTemplate = "freebox://docs/en/domain-parental-write", Name = "freebox-docs-en-domain-parental-write", Title = "Parental controls write guide (English)", MimeType = "text/markdown")]
    public static TextResourceContents EnglishDomainParentalWrite() => Get("freebox://docs/en/domain-parental-write");

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

    [McpServerResource(UriTemplate = "freebox://docs/fr/domaine-reseau-avance", Name = "freebox-docs-fr-domaine-reseau-avance", Title = "Guide avancé du domaine réseau (français)", MimeType = "text/markdown")]
    public static TextResourceContents FrenchDomainReseauAvance() => Get("freebox://docs/fr/domaine-reseau-avance");

    [McpServerResource(UriTemplate = "freebox://docs/fr/domaine-fichiers-avances", Name = "freebox-docs-fr-domaine-fichiers-avances", Title = "Guide avancé du domaine fichiers (français)", MimeType = "text/markdown")]
    public static TextResourceContents FrenchDomainFichiersAvances() => Get("freebox://docs/fr/domaine-fichiers-avances");

    [McpServerResource(UriTemplate = "freebox://docs/fr/domaine-systemhome-controles", Name = "freebox-docs-fr-domaine-systemhome-controles", Title = "Guides des contrôles système et domicile (français)", MimeType = "text/markdown")]
    public static TextResourceContents FrenchDomainSystemHomeControles() => Get("freebox://docs/fr/domaine-systemhome-controles");

    [McpServerResource(UriTemplate = "freebox://docs/fr/domaine-services-controles", Name = "freebox-docs-fr-domaine-services-controles", Title = "Guide des contrôles de services (français)", MimeType = "text/markdown")]
    public static TextResourceContents FrenchDomainServicesControles() => Get("freebox://docs/fr/domaine-services-controles");

    [McpServerResource(UriTemplate = "freebox://docs/fr/domaine-parental-ecriture", Name = "freebox-docs-fr-domaine-parental-ecriture", Title = "Guide d'écriture du contrôle parental (français)", MimeType = "text/markdown")]
    public static TextResourceContents FrenchDomainParentalEcriture() => Get("freebox://docs/fr/domaine-parental-ecriture");

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
