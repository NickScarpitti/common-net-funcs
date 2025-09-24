using System.Security.Cryptography;
using System.Text;
using static System.Math;
using static System.Random;

namespace CommonNetFuncs.Core;

/// <summary>
/// Helper functions for randomization
/// </summary>
public static class Random
{
  public static readonly RandomNumberGenerator rng = RandomNumberGenerator.Create();

  /// <summary>
    /// Generate a random integer between 0 and maxValue - 1
    /// </summary>
    /// <param name="minValue">Min value (inclusive) to return. Must be greater >= 0</param>
    /// <param name="maxValue">Max value (non-inclusive) to return</param>
    /// <returns>Random number between minValue and maxValue - 1</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if maxValue is less than or equal to 0.</exception>
    /// <exception cref="ArgumentException">Thrown if minValue is greater than maxValue.</exception>
  public static int GetRandomInt(int minValue = 0, int maxValue = int.MaxValue)
  {
    if (maxValue <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(maxValue), "Max value must be greater than 0.");
    }

    if (minValue > maxValue)
    {
      throw new ArgumentException($"{nameof(minValue)} must be less than or equal to {nameof(maxValue)}", nameof(minValue));
    }

    if (minValue == maxValue)
    {
      return minValue; //There is only one possible value, so just return that
    }

    //using RandomNumberGenerator rng = RandomNumberGenerator.Create();
    byte[] randomNumber = new byte[4]; // 4 bytes for an integer
    rng.GetBytes(randomNumber);
    int result = BitConverter.ToInt32(randomNumber, 0) & 0x7FFFFFFF; // Ensure it's non-negative
    return minValue + (result % (maxValue - minValue));
  }

  /// <summary>
    /// Generate a random integer between 0 and maxValue - 1
    /// </summary>
    /// <param name="maxValue">Max value (non-inclusive) to return</param>
    /// <returns>Random number between 0 and maxValue - 1</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if maxValue is less than or equal to 0.</exception>
    /// <exception cref="ArgumentException">Thrown if minValue is greater than maxValue.</exception>
  public static int GetRandomInt(int maxValue = int.MaxValue)
  {
    return GetRandomInt(0, maxValue);
  }

  /// <summary>
    /// Generate a random integer between 0 and int.MaxValue - 1
    /// </summary>
    /// <returns>Random number between 0 and int.MaxValue - 1</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if maxValue is less than or equal to 0.</exception>
    /// <exception cref="ArgumentException">Thrown if minValue is greater than maxValue.</exception>
  public static int GetRandomInt()
  {
    return GetRandomInt(0, int.MaxValue);
  }

  /// <summary>
    /// Generate a number of random integers between minValue and maxValue - 1
    /// </summary>
    /// <param name="numberToGenerate">The number of random integers to generate.</param>
    /// <param name="minValue">Min value (inclusive) to return in result. Must be greater >= 0</param>
    /// <param name="maxValue">Max value (non-inclusive) to return in result</param>
    /// <param name="cancellationToken">The cancellation token for this operation.</param>
    /// <returns>An enumerable of random number between minValue and maxValue - 1</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if numberToGenerate or maxValue is less than or equal to 0.</exception>
    /// <exception cref="ArgumentException">Thrown if minValue is greater than maxValue.</exception>
  public static IEnumerable<int> GetRandomInts(int numberToGenerate, int minValue = 0, int maxValue = int.MaxValue, CancellationToken cancellationToken = default)
  {
    if (numberToGenerate <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(numberToGenerate), "Number to generate must be greater than 0.");
    }

    return Enumerate();

    IEnumerable<int> Enumerate()
    {
      for (int i = 0; i < numberToGenerate; i++)
      {
        cancellationToken.ThrowIfCancellationRequested();
        yield return GetRandomInt(minValue, maxValue);
      }
    }
  }

  /// <summary>
    /// Generates a random, 15 decimal place double with no whole number component.
    /// </summary>
    /// <returns>A random 15 decimal place double with no whole number component.</returns>
  public static double GetRandomDouble()
  {
    //using RandomNumberGenerator rng = RandomNumberGenerator.Create();
    byte[] randomNumber = new byte[8]; // 8 bytes for a double
    rng.GetBytes(randomNumber);
    ulong ulongResult = BitConverter.ToUInt64(randomNumber, 0);
    return ulongResult / (double)ulong.MaxValue; // Normalize to [0.0, 1.0)
  }

  /// <summary>
    /// Generates a random double with the desired number of decimal places with no whole number component.
    /// </summary>
    /// <param name="decimalPlaces">Number of decimal places to include in the result (max of 15)</param>
    /// <returns>A random double with the desired number of decimal places with no whole number component.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if decimalPlaces is less than or equal to 0.</exception>
  public static double GetRandomDouble(int decimalPlaces = 15)
  {
    if (decimalPlaces <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "decimalPlaces must be greater than 0.");
    }

    decimalPlaces = decimalPlaces <= 15 ? decimalPlaces : 15;
    double result = GetRandomDouble();
    return Round(result, decimalPlaces, MidpointRounding.AwayFromZero);
  }

  /// <summary>
    /// Generates a number of random doubles with the desired number of decimal places with no whole number component.
    /// </summary>
    /// <param name="numberToGenerate">Number of random doubles to generate.</param>
    /// <param name="decimalPlaces">Number of decimal places to include in the results (max of 15)</param>
    /// <param name="cancellationToken">Cancellation token for this operation.</param>
    /// <returns>A random double with the desired number of decimal places with no whole number component.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if numberToGenerate is less than or equal to 0.</exception>
  public static IEnumerable<double> GetRandomDoubles(int numberToGenerate, int decimalPlaces = 15, CancellationToken cancellationToken = default)
  {
    for (int i = 0; i < numberToGenerate; i++)
    {
      cancellationToken.ThrowIfCancellationRequested();
      yield return GetRandomDouble(decimalPlaces);
    }
  }

  /// <summary>
    /// Gets a random decimal 28 decimal places long with no whole number component.
    /// </summary>
    /// <returns>A random 28 decimal place decimal with no whole number component</returns>
  public static decimal GetRandomDecimal()
  {
    //using RandomNumberGenerator rng = RandomNumberGenerator.Create();
    byte[] randomBytes = new byte[16]; // 16 bytes for higher entropy
    rng.GetBytes(randomBytes);

    // Convert the first 12 bytes to a ulong for the integer part
    ulong intPart = BitConverter.ToUInt64(randomBytes, 0) % 1000000000000000000; // Limit to 10^18
    uint fracPart = BitConverter.ToUInt32(randomBytes, 8); // Convert the next 4 bytes to a uint for the fractional part

    // Combine the parts to create a decimal in the range [0, 1)
    return (intPart / 1000000000000000000m) + ((decimal)fracPart / uint.MaxValue / 1000000000000000000m);
  }

  /// <summary>
    /// Gets a random decimal with the specified number of decimal places with no whole number component.
    /// </summary>
    /// <param name="decimalPlaces">Number of decimal places to include in the randomly generated number (max of 28)</param>
    /// <returns>Random decimal with the specified number of decimal places with no whole number component.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if decimalPlaces is less than or equal to 0.</exception>
  public static decimal GetRandomDecimal(int decimalPlaces = 28)
  {
    if (decimalPlaces <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "decimalPlaces must be greater than 0.");
    }
    decimalPlaces = decimalPlaces <= 28 ? decimalPlaces : 28;
    decimal result = GetRandomDecimal();
    return Round(result, decimalPlaces, MidpointRounding.AwayFromZero);
  }

  /// <summary>
    /// Gets a number of random decimals with the specified number of decimal places with no whole number component.
    /// </summary>
    /// <param name="numberToGenerate">Number of random decimals to generate.</param>
    /// <param name="decimalPlaces">Number of decimal places to include in the randomly generated numbers (max of 28)</param>
    /// <param name="cancellationToken">Cancellation token for this operation.</param>
    /// <returns>An enumerable of random decimals with the specified number of decimal places with no whole number component.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if numberToGenerate or is less than or equal to 0.</exception>
  public static IEnumerable<decimal> GetRandomDecimals(int numberToGenerate, int decimalPlaces = 28, CancellationToken cancellationToken = default)
  {
    for (int i = 0; i < numberToGenerate; i++)
    {
      cancellationToken.ThrowIfCancellationRequested();
      yield return GetRandomDecimal(decimalPlaces);
    }
  }

  /// <summary>
    /// Randomly shuffle a list of objects in place.
    /// </summary>
    /// <typeparam name="T">Type of objects being shuffled.</typeparam>
    /// <param name="list">List of objects to shuffle.</param>
    /// <param name="cancellationToken">The cancellation token for this operation.</param>
    /// <returns>Shuffled list of items.</returns>
  public static IList<T> ShuffleListInPlace<T>(this IList<T> list, CancellationToken cancellationToken = default)
  {
    int n = list.Count;
    while (n > 1)
    {
      cancellationToken.ThrowIfCancellationRequested();
      n--;
      int k = GetRandomInt(0, n + 1); //rngOld.Next(n + 1);
      (list[n], list[k]) = (list[k], list[n]);
    }
    return list;
  }

  /// <summary>
    /// Randomly shuffle a collection of objects using the Random.Shared.Shuffle method
    /// </summary>
    /// <typeparam name="T">Type of objects being shuffled</typeparam>
    /// <param name="items">Items to shuffle</param>
    /// <returns>Shuffled IEnumerable of items</returns>
  public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> items)
  {
    T[] arr = items.ToArray();
    Shared.Shuffle(arr);
    return arr;
  }

  /// <summary>
    /// Randomly shuffle a collection of objects using the Random.Shared.Shuffle method
    /// </summary>
    /// <typeparam name="T">Type of objects being shuffled</typeparam>
    /// <param name="items">Items to shuffle</param>
    /// <returns>Shuffled IEnumerable of items</returns>
  public static List<T> Shuffle<T>(this IList<T> items)
  {
    T[] arr = items.ToArray();
    Shared.Shuffle(arr);
    return arr.ToList();
  }

  /// <summary>
    /// Randomly shuffle an array of objects in place using the Random.Shared.Shuffle method
    /// </summary>
    /// <typeparam name="T">Type of objects being shuffled</typeparam>
    /// <param name="items">Items to shuffle</param>
    /// <returns>Shuffled IEnumerable of items</returns>
  public static void Shuffle<T>(this T[] items)
  {
    Shared.Shuffle(items);
  }

  /// <summary>
    /// Randomly shuffle a span of objects in place using the Random.Shared.Shuffle method
    /// </summary>
    /// <typeparam name="T">Type of objects being shuffled</typeparam>
    /// <param name="items">Items to shuffle</param>
    /// <returns>Shuffled IEnumerable of items</returns>
  public static void Shuffle<T>(this Span<T> items)
  {
    Shared.Shuffle(items);
  }

  /// <summary>
    /// Randomly shuffle a collection of objects in place using linq
    /// </summary>
    /// <typeparam name="T">Type of objects being shuffled</typeparam>
    /// <param name="items">Items to shuffle</param>
    /// <returns>Shuffled IEnumerable of items</returns>
  public static IEnumerable<T> ShuffleLinq<T>(this IEnumerable<T> items)
  {
    return items.OrderBy(_ => GetRandomInt());
  }

  /// <summary>
    /// Select a random object from a IEnumerable of objects
    /// </summary>
    /// <typeparam name="T">Type of object to return</typeparam>
    /// <param name="items">Items to select from</param>
    /// <returns>Randomly selected object</returns>
  public static T? GetRandomElement<T>(this IEnumerable<T> items)
  {
    return items.Skip(GetRandomInt(0, items.Count())).First();
  }

  /// <summary>
    /// Select a random object from a IEnumerable of objects
    /// </summary>
    /// <typeparam name="T">Type of objects being shuffled</typeparam>
    /// <param name="items">Items to select from</param>
    /// <param name="selectQuantity">Number of items to select</param>
    /// <returns>Randomly selected objects</returns>
  public static IEnumerable<T> GetRandomElements<T>(this IEnumerable<T> items, int selectQuantity = 1)
  {
    return Shared.GetItems(items.ToArray(), selectQuantity);
  }

  /// <summary>
    /// Select unique random objects from an <see cref="IEnumerable{T}"/> of objects.
    /// </summary>
    /// <typeparam name="T">Type of objects being shuffled.</typeparam>
    /// <param name="items">Items to select from.</param>
    /// <param name="selectQuantity">Maximum number of items to select. If there are fewer unique elements than this value, <paramref name="items"/> is simply shuffled and retuned.</param>
    /// <returns><see cref="IEnumerable{T}"/> of randomly selected objects no larger than <paramref name="selectQuantity"/>.</returns>
    /// <exception cref="ArgumentException">Throws when selectQuantity is less than 1.</exception>
  public static IEnumerable<T> GetUniqueRandomElements<T>(this IEnumerable<T> items, int selectQuantity = 1)
  {
    if (selectQuantity < 1)
    {
      throw new ArgumentException($"{nameof(selectQuantity)} must be greater than 0", nameof(selectQuantity));
    }

    return Enumerate();

    IEnumerable<T> Enumerate()
    {
      if (!items.Any())
      {
        yield break;
      }

      HashSet<T> allUniqueItems = new(items);
      if (selectQuantity >= allUniqueItems.Count)
      {
        foreach (T item in allUniqueItems.Shuffle())
        {
          yield return item;
        }
        yield break;
      }

      T[] uniqueList = allUniqueItems.ToArray();
      int availableCount = uniqueList.Length;

      // Use reservoir sampling approach for efficiency
      for (int selectedCounter = 0; selectedCounter < selectQuantity; selectedCounter++)
      {
        int randomIndex = GetRandomInt(availableCount);
        yield return uniqueList[randomIndex];

        // Move used item to end of array and then exclude it from the available indexes by reducing availableCount by 1
        (uniqueList[randomIndex], uniqueList[availableCount - 1]) = (uniqueList[availableCount - 1], uniqueList[randomIndex]);
        availableCount--;
      }
    }
  }

  /// <summary>
    /// Generates a random string of the indicated length using a range of ASCII characters.
    /// </summary>
    /// <param name="maxLength">Upper bound for length of strings to be generated.</param>
    /// <param name="minLength">Lower bound for length of strings to be generated.</param>
    /// <param name="lowerAsciiBound">First decimal number for an ASCII character (inclusive) to use (max 126).</param>
    /// <param name="upperAsciiBound">Last decimal number for an ASCII character (inclusive) to use (max 126).</param>
    /// <param name="blacklistedCharacters">Characters that fall within the provided range that are to not be used.</param>
    /// <param name="cancellationToken">The cancellation token for this operation.</param>
    /// <returns>A random string of the given length comprised only of characters within the range of ASCII characters provided, and excluding any in the black list</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any of the provided bounds are invalid.</exception>
  public static string GenerateRandomString(int maxLength, int minLength = -1, int lowerAsciiBound = 32, int upperAsciiBound = 126, char[]? blacklistedCharacters = null, CancellationToken cancellationToken = default)
  {
    if (lowerAsciiBound < 0 || upperAsciiBound > 127 || lowerAsciiBound >= upperAsciiBound)
    {
      throw new ArgumentOutOfRangeException(nameof(upperAsciiBound), "Bounds must be between 0 and 127, and lowerBound must be less than upperBound.");
    }

    if (maxLength < minLength)
    {
      throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be greater than or equal to min length.");
    }

    if (maxLength <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be greater than 0");
    }

    int length = minLength == -1 ? maxLength : GetRandomInt(minLength, maxLength);

    StringBuilder result = new(length);

    if (blacklistedCharacters == null || blacklistedCharacters.Length == 0)
    {
      for (int i = 0; i < length; i++)
      {
        result.Append((char)GetRandomInt(lowerAsciiBound, upperAsciiBound + 1));
      }
    }
    else
    {
      HashSet<int> blackListCharVals = blacklistedCharacters.Select(x => (int)x).ToHashSet();
      IEnumerable<int> whiteListCharVals = Enumerable.Range(lowerAsciiBound, upperAsciiBound - lowerAsciiBound);
      if (whiteListCharVals.Intersect(blackListCharVals).Count() == whiteListCharVals.Count())
      {
        throw new Exception("Black list contains all available values");
      }

      // Generate the first part of the string with a for loop for speed
      for (int i = 0; i < length; i++)
      {
        cancellationToken.ThrowIfCancellationRequested();
        int randomInt = GetRandomInt(lowerAsciiBound, upperAsciiBound + 1);
        if (!blackListCharVals.Contains(randomInt))
        {
          result.Append((char)randomInt);
        }
        else
        {
          i--; // If the character is blacklisted, decrement i to try again
        }
      }
    }

    return result.ToString();
  }

  /// <summary>
    /// Generates a number of random strings of the indicated length using a range of ASCII characters
    /// </summary>
    /// <param name="numberToGenerate">Number of random strings to be generated.</param>
    /// <param name="maxLength">Upper bound for length of strings to be generated.</param>
    /// <param name="minLength">Lower bound for length of strings to be generated.</param>
    /// <param name="lowerAsciiBound">First decimal number for an ASCII character (inclusive) to use (max 126).</param>
    /// <param name="upperAsciiBound">Last decimal number for an ASCII character (inclusive) to use (max 126).</param>
    /// <param name="blacklistedCharacters">Characters that fall within the provided range that are to not be used.</param>
    /// <param name="cancellationToken">The cancellation token for this operation.</param>
    /// <returns>An enumerable of random strings of the given length comprised only of characters within the range of ASCII characters provided, and excluding any in the black list</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if any of the provided bounds are invalid.</exception>
  public static IEnumerable<string> GenerateRandomStrings(int numberToGenerate, int maxLength, int minLength = -1, int lowerAsciiBound = 32, int upperAsciiBound = 126, char[]? blacklistedCharacters = null, CancellationToken cancellationToken = default)
  {
    for (int i = 0; i < numberToGenerate; i++)
    {
      yield return GenerateRandomString(maxLength, minLength, lowerAsciiBound, upperAsciiBound, blacklistedCharacters, cancellationToken);
    }
  }

  internal static readonly char[] DefaultCharSet = [ 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' ];

  /// <summary>
    /// Generates a random string of the indicated length using either a custom character set, or the default of a-z A-Z 1-9.
    /// </summary>
    /// <param name="length">Length of the random string to be generated.</param>
    /// <param name="charSet">Characters that are to be used in the generated string.</param>
    /// <param name="cancellationToken">The cancellation token for this operation.</param>
    /// <returns>A random string of the given length comprised only of characters in either the default or custom character set</returns>
  public static string GenerateRandomStringByCharSet(int length, char[]? charSet = null, CancellationToken cancellationToken = default)
  {
    // Use a default character set if none is provided
    if (charSet == null || charSet.Length == 0)
    {
      charSet = DefaultCharSet;
    }

    StringBuilder result = new(length);

    for (int i = 0; i < length; i++)
    {
      cancellationToken.ThrowIfCancellationRequested();
      char randomChar = charSet[GetRandomInt(0, charSet.Length)];
      result.Append(randomChar);
    }

    return result.ToString();
  }
}
