namespace Freenaute.Freebox.Mcp.Resources.Documentation;

public static class DocumentationStore
{
    private const int MaxDocumentCharacters = 64 * 1024;

    // Manifest resource names produced by the csproj LogicalName metadata (docs/<lang>/<name>.md).
    private const string EnglishOverviewResourceName = "docs/en/overview.md";
    private const string EnglishResourcesResourceName = "docs/en/resources.md";
    private const string EnglishQuickstartResourceName = "docs/en/quickstart.md";
    private const string EnglishAuthPairingResourceName = "docs/en/auth-pairing.md";
    private const string EnglishToolFamiliesResourceName = "docs/en/tool-families.md";
    private const string FrenchOverviewResourceName = "docs/fr/overview.md";
    private const string FrenchResourcesResourceName = "docs/fr/resources.md";
    private const string FrenchQuickstartResourceName = "docs/fr/quickstart.md";
    private const string FrenchAuthPairingResourceName = "docs/fr/authentification-appairage.md";
    private const string FrenchToolFamiliesResourceName = "docs/fr/tool-families.md";
    private const string EnglishDomainNetworkAdvancedResourceName = "docs/en/domain-network-advanced.md";
    private const string EnglishDomainFilesAdvancedResourceName = "docs/en/domain-files-advanced.md";
    private const string EnglishDomainSystemHomeControlsResourceName = "docs/en/domain-systemhome-controls.md";
    private const string EnglishDomainServicesControlsResourceName = "docs/en/domain-services-controls.md";
    private const string EnglishDomainParentalWriteResourceName = "docs/en/domain-parental-write.md";
    private const string FrenchDomainReseauAvanceResourceName = "docs/fr/domaine-reseau-avance.md";
    private const string FrenchDomainFichiersAvancesResourceName = "docs/fr/domaine-fichiers-avances.md";
    private const string FrenchDomainSystemHomeControlesResourceName = "docs/fr/domaine-systemhome-controles.md";
    private const string FrenchDomainServicesControlesResourceName = "docs/fr/domaine-services-controles.md";
    private const string FrenchDomainParentalEcritureResourceName = "docs/fr/domaine-parental-ecriture.md";

    private static readonly FrozenDictionary<string, string> Resources =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["freebox://docs/en/overview"] = Load(EnglishOverviewResourceName),
            ["freebox://docs/en/resources"] = Load(EnglishResourcesResourceName),
            ["freebox://docs/en/quickstart"] = Load(EnglishQuickstartResourceName),
            ["freebox://docs/en/auth-pairing"] = Load(EnglishAuthPairingResourceName),
            ["freebox://docs/en/tool-families"] = Load(EnglishToolFamiliesResourceName),
            ["freebox://docs/fr/overview"] = Load(FrenchOverviewResourceName),
            ["freebox://docs/fr/resources"] = Load(FrenchResourcesResourceName),
            ["freebox://docs/fr/quickstart"] = Load(FrenchQuickstartResourceName),
            ["freebox://docs/fr/authentification-appairage"] = Load(FrenchAuthPairingResourceName),
            ["freebox://docs/fr/tool-families"] = Load(FrenchToolFamiliesResourceName),
            ["freebox://docs/en/domain-network-advanced"] = Load(EnglishDomainNetworkAdvancedResourceName),
            ["freebox://docs/en/domain-files-advanced"] = Load(EnglishDomainFilesAdvancedResourceName),
            ["freebox://docs/en/domain-systemhome-controls"] = Load(EnglishDomainSystemHomeControlsResourceName),
            ["freebox://docs/en/domain-services-controls"] = Load(EnglishDomainServicesControlsResourceName),
            ["freebox://docs/en/domain-parental-write"] = Load(EnglishDomainParentalWriteResourceName),
            ["freebox://docs/fr/domaine-reseau-avance"] = Load(FrenchDomainReseauAvanceResourceName),
            ["freebox://docs/fr/domaine-fichiers-avances"] = Load(FrenchDomainFichiersAvancesResourceName),
            ["freebox://docs/fr/domaine-systemhome-controles"] = Load(FrenchDomainSystemHomeControlesResourceName),
            ["freebox://docs/fr/domaine-services-controles"] = Load(FrenchDomainServicesControlesResourceName),
            ["freebox://docs/fr/domaine-parental-ecriture"] = Load(FrenchDomainParentalEcritureResourceName)
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static bool TryGet(string uri, out string markdown)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return Resources.TryGetValue(uri, out markdown!);
    }

    private static string Load(string resourceName)
    {
        // AOT-safe: typeof(...).Assembly + GetManifestResourceStream involve no reflection over types.
        Assembly assembly = typeof(DocumentationStore).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
                              ?? throw new InvalidOperationException(
                                  $"Embedded documentation resource '{resourceName}' was not found.");
        using StreamReader reader = new(stream, leaveOpen: false);
        string markdown = reader.ReadToEnd();

        return markdown.Length <= MaxDocumentCharacters
            ? markdown
            : markdown[..MaxDocumentCharacters] + "\n\n[documentation truncated]\n";
    }
}
