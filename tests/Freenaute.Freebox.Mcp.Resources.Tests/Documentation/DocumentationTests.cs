namespace Freenaute.Freebox.Mcp.Resources.Tests.Documentation;

public sealed class DocumentationTests
{
    public static TheoryData<string> DocumentUris =>
    [
        "freebox://docs/en/overview",
        "freebox://docs/en/resources",
        "freebox://docs/en/quickstart",
        "freebox://docs/en/auth-pairing",
        "freebox://docs/en/tool-families",
        "freebox://docs/fr/overview",
        "freebox://docs/fr/resources",
        "freebox://docs/fr/quickstart",
        "freebox://docs/fr/authentification-appairage",
        "freebox://docs/fr/tool-families",
        "freebox://docs/en/domain-network-advanced",
        "freebox://docs/en/domain-files-advanced",
        "freebox://docs/en/domain-systemhome-controls",
        "freebox://docs/en/domain-services-controls",
        "freebox://docs/en/domain-parental-write",
        "freebox://docs/fr/domaine-reseau-avance",
        "freebox://docs/fr/domaine-fichiers-avances",
        "freebox://docs/fr/domaine-systemhome-controles",
        "freebox://docs/fr/domaine-services-controles",
        "freebox://docs/fr/domaine-parental-ecriture"
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
    [InlineData("freebox://docs/en/overview", "Discovery")]
    [InlineData("freebox://docs/en/overview", "Pairing")]
    [InlineData("freebox://docs/en/overview", "Network")]
    [InlineData("freebox://docs/en/overview", "System")]
    [InlineData("freebox://docs/en/resources", "## Tool families")]
    [InlineData("freebox://docs/en/resources", "freebox_pairing_wait")]
    [InlineData("freebox://docs/en/resources", "includeHosts")]
    [InlineData("freebox://docs/en/quickstart", "Tool-family routing")]
    [InlineData("freebox://docs/en/quickstart", "pollIntervalSeconds")]
    [InlineData("freebox://docs/en/auth-pairing", "Safe operating sequence")]
    [InlineData("freebox://docs/en/auth-pairing", "physical confirmation")]
    [InlineData("freebox://docs/en/tool-families", "Downloads family")]
    [InlineData("freebox://docs/en/tool-families", "Calls family")]
    [InlineData("freebox://docs/en/tool-families", "Contacts family")]
    [InlineData("freebox://docs/en/tool-families", "Cameras family")]
    [InlineData("freebox://docs/en/tool-families", "Notifications family")]
    [InlineData("freebox://docs/en/tool-families", "AirMedia family")]
    [InlineData("freebox://docs/en/tool-families", "Shares family")]
    [InlineData("freebox://docs/en/tool-families", "Parental controls family")]
    [InlineData("freebox://docs/fr/tool-families", "Famille Téléchargements")]
    [InlineData("freebox://docs/fr/tool-families", "Famille AirMedia")]
    [InlineData("freebox://docs/fr/tool-families", "Famille Contrôle parental")]
    public void TryGet_EnglishDocumentation_ContainsToolFamilyGuidance(string uri, string expectedText)
    {
        Assert.True(DocumentationStore.TryGet(uri, out string markdown));
        Assert.Contains(expectedText, markdown, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Découverte", "freebox_discover")]
    [InlineData("Appairage", "freebox_pairing_wait")]
    [InlineData("Réseau", "freebox_wifi_status")]
    [InlineData("Système", "freebox_system_status")]
    public void FrenchOverview_CoversToolFamilyAndTool(string heading, string toolName)
    {
        Assert.True(DocumentationStore.TryGet("freebox://docs/fr/overview", out string markdown));
        Assert.Contains(heading, markdown, StringComparison.Ordinal);
        Assert.Contains(toolName, markdown, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Téléchargements", "freebox_downloads_list")]
    [InlineData("Appels téléphoniques", "freebox_calls_clear_log")]
    [InlineData("Contacts", "freebox_contacts_search")]
    [InlineData("Caméras et domicile", "freebox_cameras_overview")]
    [InlineData("Notifications et flux", "freebox_notifications_feed")]
    [InlineData("AirMedia", "freebox_airmedia_receivers")]
    [InlineData("Contrôle parental", "freebox_parental_filters")]
    [InlineData("NAS et partages", "freebox_shares_visibility")]
    public void FrenchResources_CoversAllAdditionalToolFamilies(string heading, string toolName)
    {
        Assert.True(DocumentationStore.TryGet("freebox://docs/fr/resources", out string markdown));
        Assert.Contains(heading, markdown, StringComparison.Ordinal);
        Assert.Contains(toolName, markdown, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(nameof(DocumentationResources.EnglishOverview), "freebox://docs/en/overview",
        "freebox-docs-en-overview", "Freebox MCP overview (English)")]
    [InlineData(nameof(DocumentationResources.EnglishResourceCatalog), "freebox://docs/en/resources",
        "freebox-docs-en-resources", "Freebox resource catalog (English)")]
    [InlineData(nameof(DocumentationResources.EnglishQuickstart), "freebox://docs/en/quickstart",
        "freebox-docs-en-quickstart", "Freebox MCP quickstart (English)")]
    [InlineData(nameof(DocumentationResources.EnglishAuthPairing), "freebox://docs/en/auth-pairing",
        "freebox-docs-en-auth-pairing", "Freebox authentication and pairing (English)")]
    [InlineData(nameof(DocumentationResources.EnglishToolFamilies), "freebox://docs/en/tool-families",
        "freebox-docs-en-tool-families", "Freebox MCP tool-family guide (English)")]
    [InlineData(nameof(DocumentationResources.FrenchOverview), "freebox://docs/fr/overview", "freebox-docs-fr-overview",
        "Présentation MCP Freebox (français)")]
    [InlineData(nameof(DocumentationResources.FrenchResourceCatalog), "freebox://docs/fr/resources",
        "freebox-docs-fr-resources", "Catalogue des ressources Freebox (français)")]
    [InlineData(nameof(DocumentationResources.FrenchQuickstart), "freebox://docs/fr/quickstart",
        "freebox-docs-fr-quickstart", "Démarrage rapide MCP Freebox (français)")]
    [InlineData(nameof(DocumentationResources.FrenchAuthPairing), "freebox://docs/fr/authentification-appairage",
        "freebox-docs-fr-authentification-appairage", "Authentification et appairage Freebox (français)")]
    [InlineData(nameof(DocumentationResources.FrenchToolFamilies), "freebox://docs/fr/tool-families",
        "freebox-docs-fr-tool-families", "Guide des familles d'outils MCP Freebox (français)")]
    [InlineData(nameof(DocumentationResources.EnglishDomainNetworkAdvanced), "freebox://docs/en/domain-network-advanced",
        "freebox-docs-en-domain-network-advanced", "Advanced network domain guide (English)")]
    [InlineData(nameof(DocumentationResources.EnglishDomainFilesAdvanced), "freebox://docs/en/domain-files-advanced",
        "freebox-docs-en-domain-files-advanced", "Advanced files domain guide (English)")]
    [InlineData(nameof(DocumentationResources.EnglishDomainSystemHomeControls), "freebox://docs/en/domain-systemhome-controls",
        "freebox-docs-en-domain-systemhome-controls", "System and home controls guide (English)")]
    [InlineData(nameof(DocumentationResources.EnglishDomainServicesControls), "freebox://docs/en/domain-services-controls",
        "freebox-docs-en-domain-services-controls", "Services controls guide (English)")]
    [InlineData(nameof(DocumentationResources.EnglishDomainParentalWrite), "freebox://docs/en/domain-parental-write",
        "freebox-docs-en-domain-parental-write", "Parental controls write guide (English)")]
    [InlineData(nameof(DocumentationResources.FrenchDomainReseauAvance), "freebox://docs/fr/domaine-reseau-avance",
        "freebox-docs-fr-domaine-reseau-avance", "Guide avancé du domaine réseau (français)")]
    [InlineData(nameof(DocumentationResources.FrenchDomainFichiersAvances), "freebox://docs/fr/domaine-fichiers-avances",
        "freebox-docs-fr-domaine-fichiers-avances", "Guide avancé du domaine fichiers (français)")]
    [InlineData(nameof(DocumentationResources.FrenchDomainSystemHomeControles), "freebox://docs/fr/domaine-systemhome-controles",
        "freebox-docs-fr-domaine-systemhome-controles", "Guides des contrôles système et domicile (français)")]
    [InlineData(nameof(DocumentationResources.FrenchDomainServicesControles), "freebox://docs/fr/domaine-services-controles",
        "freebox-docs-fr-domaine-services-controles", "Guide des contrôles de services (français)")]
    [InlineData(nameof(DocumentationResources.FrenchDomainParentalEcriture), "freebox://docs/fr/domaine-parental-ecriture",
        "freebox-docs-fr-domaine-parental-ecriture", "Guide d'écriture du contrôle parental (français)")]
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
