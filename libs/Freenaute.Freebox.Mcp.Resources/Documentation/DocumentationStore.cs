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
            ["freebox://docs/fr/tool-families"] = Load(FrenchToolFamiliesResourceName)
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
