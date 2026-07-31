#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill required by the compiler to allow use of <c>init</c> accessors and positional <c>record</c> types on
/// target frameworks older than .NET 5, which is where this marker type was added to the BCL.
/// </summary>
internal static class IsExternalInit;
#endif
