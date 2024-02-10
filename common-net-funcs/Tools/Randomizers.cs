using static System.Random;
namespace Common_Net_Funcs.Tools;

/// <summary>
/// Helper functions for randomization
/// </summary>
public static class Randomizers
{
    private static readonly Random rng = new();

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
            int k = rng.Next(n + 1);
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
        return items.OrderBy(_ => rng.Next()).ToList();
    }

    /// <summary>
    /// Select a random object from a IEnumerable of objects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">Items to select from</param>
    /// <returns>Randomly selected object</returns>
    public static T GetRandomElement<T>(this IEnumerable<T> items)
    {
        return items.Skip(rng.Next(items.Count())).First();
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
