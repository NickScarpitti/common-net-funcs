using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CommonNetFuncs.Web.Interface;

public static class SelectListItemCreation
{
	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for both Value and Text properties.</param>
	/// <returns>SelectListItem with text and value properties set to the passed in value</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static SelectListItem? ToSelectListItem(this string? value, bool selected)
	{
		return value != null ? new() { Value = value, Text = value, Selected = selected } : null;
	}

	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for both Value and Text properties.</param>
	/// <returns>SelectListItem with text and value properties set to the passed in value</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static SelectListItem? ToSelectListItem(this string? value)
	{
		return value != null ? new() { Value = value, Text = value } : null;
	}

	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for the Value property</param>
	/// <param name="text">Value to be used for the Text property</param>
	/// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
	[return: NotNullIfNotNull(nameof(value)), NotNullIfNotNull(nameof(text))]
	public static SelectListItem? ToSelectListItem(this string? value, string? text, bool selected)
	{
		return value != null ? new() { Value = value, Text = text ?? string.Empty, Selected = selected } : null;
	}

	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for the Value property</param>
	/// <param name="text">Value to be used for the Text property</param>
	/// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static SelectListItem? ToSelectListItem(this string? value, string? text)
	{
		return value != null ? new() { Value = value, Text = text ?? string.Empty } : null;
	}

	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for both Value and Text properties.</param>
	/// <returns>SelectListItem with text and value properties set to the passed in value</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static SelectListItem ToSelectListItem<TNumber>(this TNumber value, bool selected) where TNumber : struct, INumber<TNumber>
	{
		return new() { Value = value.ToString(), Text = value.ToString(), Selected = selected };
	}

	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for both Value and Text properties.</param>
	/// <returns>SelectListItem with text and value properties set to the passed in value</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static SelectListItem ToSelectListItem<TNumber>(this TNumber value) where TNumber : struct, INumber<TNumber>
	{
		return new() { Value = value.ToString(), Text = value.ToString() };
	}

	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for the Value property</param>
	/// <param name="text">Value to be used for the Text property</param>
	/// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static SelectListItem ToSelectListItem<TNumber>(this TNumber value, string? text, bool selected) where TNumber : struct, INumber<TNumber>
	{
		return new() { Value = value.ToString(), Text = text ?? string.Empty, Selected = selected };
	}

	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for the Value property</param>
	/// <param name="text">Value to be used for the Text property</param>
	/// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static SelectListItem ToSelectListItem<TNumber>(this TNumber value, string? text) where TNumber : struct, INumber<TNumber>
	{
		return new() { Value = value.ToString(), Text = text ?? string.Empty };
	}

	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for both Value and Text properties.</param>
	/// <returns>SelectListItem with text and value properties set to the passed in value</returns>
	public static SelectListItem? ToSelectListItem<TNumber>(this TNumber? value, bool selected) where TNumber : struct, INumber<TNumber>
	{
		return value != null ? new() { Value = value.ToString(), Text = value.ToString(), Selected = selected } : null;
	}

	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for both Value and Text properties.</param>
	/// <returns>SelectListItem with text and value properties set to the passed in value</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static SelectListItem? ToSelectListItem<TNumber>(this TNumber? value) where TNumber : struct, INumber<TNumber>
	{
		return value != null ? new() { Value = value.ToString(), Text = value.ToString() } : null;
	}

	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for both Value and Text properties.</param>
	/// <param name="text">Value to be used for the Text property</param>
	/// <returns>SelectListItem with text and value properties set to the passed in value</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static SelectListItem? ToSelectListItem<TNumber>(this TNumber? value, string? text) where TNumber : struct, INumber<TNumber>
	{
		return value != null ? new() { Value = value.ToString(), Text = text ?? string.Empty } : null;
	}

	/// <summary>
	/// Converts value to select list item
	/// </summary>
	/// <param name="value">Value to be used for the Value property</param>
	/// <param name="text">Value to be used for the Text property</param>
	/// <returns>SelectListItem with text and value properties set to the passed in text and value. Will use value for text if text is null</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public static SelectListItem? ToSelectListItem<TNumber>(this TNumber? value, string? text, bool selected) where TNumber : struct, INumber<TNumber>
	{
		return value != null ? new() { Value = value.ToString(), Text = text ?? string.Empty, Selected = selected } : null;
	}
}
