namespace Common_Net_Funcs.Tools;

/// <summary>
/// Helper functions for randomization
/// </summary>
public static class Randomizers
{
    private static readonly Random rng = new();

    /// <summary>
    /// Randomly shuffle a list of objects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static IList<T> Shuffle<T>(this IList<T> list)
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
    /// Randomly shuffle a list of objects using linq
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static void LinqShuffle<T>(this IList<T> list)
    {
        list = list.OrderBy(x => rng.Next()).ToList();
    }

    /// <summary>
    /// Select a random object from a IEnumerable of objects
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns>Randomly selected object</returns>
    public static T? RandomElement<T>(this IEnumerable<T> list)
    {
        return list.Skip(rng.Next(list.Count())).FirstOrDefault();
    }
}
