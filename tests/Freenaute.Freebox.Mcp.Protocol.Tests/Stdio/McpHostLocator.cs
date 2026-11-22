namespace Freenaute.Freebox.Mcp.Protocol.Tests.Stdio;

/// <summary>
///     Locates the compiled MCP host assembly produced by the BuildMcpHost contract build
///     (dotnet msbuild mcp/Freenaute.Freebox.Mcp -t:BuildMcpHost -p:Configuration=Release),
///     whose output lands under .artifacts/protocol-host/Release/.
/// </summary>
internal static class McpHostLocator
{
    private const string HostAssemblyFileName = "Freenaute.Freebox.Mcp.dll";

    public static string FindHostAssembly()
    {
        List<string> roots = [];

        // Preferred root: baked at build time from $(ArtifactsPath) (AssemblyMetadata).
        string? bakedRoot = typeof(McpHostLocator).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == "McpHostArtifactsRoot")
            ?.Value;
        if (!string.IsNullOrWhiteSpace(bakedRoot))
        {
            roots.Add(bakedRoot);
        }

        // Fallback: walk up from the test assembly to the .artifacts root.
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null &&
               !string.Equals(current.Name, ".artifacts", StringComparison.OrdinalIgnoreCase))
        {
            current = current.Parent;
        }

        if (current is not null)
        {
            roots.Add(Path.Combine(current.FullName, "protocol-host"));
        }

        foreach (string root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            string[] candidates = Directory.GetFiles(root, HostAssemblyFileName, SearchOption.AllDirectories);
            string? found = candidates.FirstOrDefault(path =>
                                path.Contains("release", StringComparison.OrdinalIgnoreCase))
                            ?? candidates.FirstOrDefault();
            if (found is not null)
            {
                return found;
            }
        }

        Assert.Fail(
            $"Managed MCP host assembly '{HostAssemblyFileName}' was not found under: {string.Join("; ", roots)}. " +
            "Run the Protocol.Tests build so the BuildMcpHost target produces .artifacts/protocol-host/Release.");
        return null!;
    }
}
