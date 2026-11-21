namespace Freenaute.Freebox.Mcp.Tools.Tests.Mapping;

public sealed class SafeHelperTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("text", "text")]
    public void Text_ReturnsNonNullString(string? input, string expected)
    {
        Assert.Equal(expected, Invoke<string>("Text", input, 128));
    }

    [Fact]
    public void Text_RemovesControlCharactersAndHonorsMaximumLength()
    {
        Assert.Equal("abc", Invoke<string>("Text", "a\0b\rc", 128));
        Assert.Equal("ab", Invoke<string>("Text", "abcd", 2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void StableIdentifier_BlankValue_ReturnsUnknown(string? input)
    {
        Assert.Equal("unknown", Invoke<string>("StableIdentifier", input));
    }

    [Fact]
    public void StableIdentifier_Value_ReturnsLowerHexSha256()
    {
        const string Value = "LAN-host-id";
        string expected = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Value)));
        Assert.Equal(expected, Invoke<string>("StableIdentifier", Value));
        Assert.NotEqual(Value, expected);
    }

    private static T? Invoke<T>(string methodName, params object?[] arguments)
    {
        Type safe = typeof(FreeboxDiscoveryTools).Assembly.GetType(
            "Freenaute.Freebox.Mcp.Tools.Security.Safe", true)!;
        MethodInfo method = safe.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public)!;
        return (T?)method.Invoke(null, arguments);
    }
}
