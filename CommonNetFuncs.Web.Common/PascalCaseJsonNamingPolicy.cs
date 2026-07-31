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

		// string.Create(int, TState, SpanAction<char, TState>) is available on netstandard2.1 too, so no TFM split is needed here.
		return string.Create(name.Length, name, (chars, value) =>
		{
			value.CopyTo(chars);
			FixCasing(chars);
		});
	}

	private static void FixCasing(Span<char> chars)
	{
		chars[0] = char.ToUpperInvariant(chars[0]);
	}
}
