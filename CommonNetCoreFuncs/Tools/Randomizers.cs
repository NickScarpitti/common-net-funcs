using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace CommonNetCoreFuncs.Tools
{
    public static class Randomizers
    {
        private static readonly Random rng = new();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void LinqShuffle<T>(this IList<T> list)
        {
            list = list.OrderBy(x => rng.Next()).ToList(); ;
        }

        public static T RandomElement<T>(this IList<T> list)
        {
            return list.Skip(rng.Next(list.Count())).FirstOrDefault();
        }        
        
        public static T RandomElement<T>(this IEnumerable<T> list)
        {
            return list.Skip(rng.Next(list.Count())).FirstOrDefault();
        }
    }
}
