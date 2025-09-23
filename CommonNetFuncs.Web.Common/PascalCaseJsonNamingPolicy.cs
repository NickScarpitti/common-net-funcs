using System.Text.Json;

namespace CommonNetFuncs.Web.Common;

public sealed class PascalCaseJsonNamingPolicy : JsonNamingPolicy
{
  public override string ConvertName(string name)
  {
    if (string.IsNullOrEmpty(name) || !char.IsLower(name[0]))
    {
      return name;
    }

    #if NETCOREAPP

    return string.Create(name.Length, name, (chars, value) =>
    {
      value.CopyTo(chars);
      FixCasing(chars);
    });

    #else

    char[] chars = name.ToCharArray();
    FixCasing(chars);
    return new string(chars);

    #endif
  }

  private static void FixCasing(Span<char> chars)
  {
    chars[0] = char.ToUpperInvariant(chars[0]);
  }
}
