using System.Security.Cryptography;
using static System.Random;
using static System.Math;

namespace Common_Net_Funcs.Tools;

/// <summary>
/// Helper functions for randomization
/// </summary>
public static class Randomizers
{
    /// <summary>
    /// Generate a random integer between 0 and maxValue - 1
    /// </summary>
    /// <param name="maxValue">Max value (non-inclusive) to return</param>
    /// <returns>Random number between 0 and maxValue - 1</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static int GetRandomInt(int maxValue = int.MaxValue)
    {
        if (maxValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxValue), "Max value must be greater than 0.");
        }

        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] randomNumber = new byte[4]; // 4 bytes for an integer
        rng.GetBytes(randomNumber);
        int result = BitConverter.ToInt32(randomNumber, 0) & 0x7FFFFFFF; // Ensure it's non-negative
        return result % maxValue; // Return a value between 0 and maxValue - 1
    }

    public static double GetRandomDouble()
    {
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] randomNumber = new byte[8]; // 8 bytes for a double
        rng.GetBytes(randomNumber);
        ulong ulongResult = BitConverter.ToUInt64(randomNumber, 0);
        return ulongResult / (double)ulong.MaxValue; // Normalize to [0.0, 1.0)
    }

    public static double GetRandomDouble(int decimalPlaces)
    {
        if (decimalPlaces <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "decimalPlaces must be greater than 0.");
        }
        decimalPlaces = decimalPlaces <= 17 ? decimalPlaces : 17;
        double result = GetRandomDouble();
        return Round(result, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    //public static double GetRandomDouble(int decimalPlaces)
    //{
    //    decimalPlaces = decimalPlaces <= 17 ? decimalPlaces : 17;
    //    double output = 0;
    //    long divisor = 1;
    //    for (int i = 1; i < decimalPlaces + 1; i++)
    //    {
    //        divisor *= 10;
    //        output += (double)GetRandomInt(10) / (long)divisor;
    //    }
    //    return output;
    //}


    public static decimal GetRandomDecimal()
    {
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] randomBytes = new byte[16]; // 16 bytes for higher entropy
        rng.GetBytes(randomBytes);

        // Convert the first 12 bytes to a ulong for the integer part
        ulong intPart = BitConverter.ToUInt64(randomBytes, 0) % 1000000000000000000; // Limit to 10^18
        uint fracPart = BitConverter.ToUInt32(randomBytes, 8); // Convert the next 4 bytes to a uint for the fractional part

        // Combine the parts to create a decimal in the range [0, 1)
        return intPart / 1000000000000000000m + (decimal)fracPart / uint.MaxValue / 1000000000000000000m;
    }

    public static decimal GetRandomDecimal(int decimalPlaces)
    {
        if (decimalPlaces <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(decimalPlaces), "decimalPlaces must be greater than 0.");
        }
        decimalPlaces = decimalPlaces <= 28 ? decimalPlaces : 28;
        decimal result = GetRandomDecimal();
        return Round(result, decimalPlaces, MidpointRounding.AwayFromZero);
    }

    //public static decimal GetRandomDecimal(int decimalPlaces)
    //{
    //    decimalPlaces = decimalPlaces <= 28 ? decimalPlaces : 28;
    //    decimal output = 0;
    //    decimal divisor = 1;
    //    for (int i = 1; i < decimalPlaces + 1; i++)
    //    {
    //        divisor *= 10;
    //        output += GetRandomInt(10) / divisor;
    //    }
    //    return output;
    //}

    /// <summary>
    /// Randomly shuffle a list of objects in place
    /// </summary>
    /// <typeparam name="T">Type of objects being shuffled</typeparam>
    /// <param name="list">List of objects to shuffle</param>
    /// <returns>Shuffled list of items</returns>
    public static IList<T> ShuffleListInPlace<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = GetRandomInt(n + 1); //rngOld.Next(n + 1);
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
        return items.OrderBy(_ => GetRandomInt()).ToList();
    }

    /// <summary>
    /// Select a random object from a IEnumerable of objects
    /// </summary>
    /// <typeparam name="T">Type of object to return</typeparam>
    /// <param name="items">Items to select from</param>
    /// <returns>Randomly selected object</returns>
    public static T GetRandomElement<T>(this IEnumerable<T> items)
    {
        return items.Skip(GetRandomInt(items.Count())).First();
    }

    /// <summary>
    /// Select a random object from a IEnumerable of objects
    /// </summary>
    /// <typeparam name="T">Type of objects being shuffled</typeparam>
    /// <param name="items">Items to select from</param>
    /// <returns>Randomly selected objects</returns>
    public static IEnumerable<T> GetRandomElements<T>(this IEnumerable<T> items, int selectQuantity = 1)
    {
        return Shared.GetItems(items.ToArray(), selectQuantity);
    }
}
