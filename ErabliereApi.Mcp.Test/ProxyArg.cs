using NSubstitute;

namespace ErabliereApi.Mcp.Test;

/// <summary>
/// The proxy methods take several parameters of the same type, and NSubstitute
/// refuses a call mixing raw values and argument matchers on those. These
/// helpers keep every argument a matcher while staying readable.
/// </summary>
internal static class ProxyArg
{
    public static string? AnyString() => Arg.Any<string?>();

    public static string? NullString() => Arg.Is<string?>(value => value == null);

    public static string? String(string expected) => Arg.Is<string?>(value => value == expected);

    public static int? AnyInt() => Arg.Any<int?>();

    public static int? NullInt() => Arg.Is<int?>(value => value == null);

    public static int? Int(int expected) => Arg.Is<int?>(value => value == expected);

    public static bool? NullBool() => Arg.Is<bool?>(value => value == null);

    public static bool? AnyBool() => Arg.Any<bool?>();
}
