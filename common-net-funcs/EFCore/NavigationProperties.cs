﻿using System.Collections.Concurrent;
using Common_Net_Funcs.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;

namespace Common_Net_Funcs.EFCore;

public static class NavigationProperties
{
    //Navigations are cached by type to prevent having to discover them every time they are needed
    static readonly ConcurrentDictionary<string, Type> cachedEntityNavigations = new();
    static readonly ConcurrentDictionary<Type, bool> completeCachedEntities = new();

    public static IQueryable<T> IncludeNavigationProperties<T>(this IQueryable<T> query, DbContext context, Type entityType, int depth = 0, int maxDepth = 100,
        Dictionary<int, string?>? parentProperties = null, Dictionary<string, Type>? foundNavigations = null, bool useCaching = true) where T : class
    {
        if (depth > maxDepth) return query;

        if (depth == 0 && useCaching && completeCachedEntities.TryGetValue(typeof(T), out _))
        {
            foreach (string includeStatement in cachedEntityNavigations.Where(x => x.Value == typeof(T)).Select(x => x.Key))
            {
                query = query.Include(includeStatement);
            }
        }
        else
        {
            parentProperties ??= new();
            foundNavigations ??= new();

            IEnumerable<INavigation> navigations = (context.Model.FindEntityType(entityType)?.GetNavigations()
                .Where(x => entityType.GetProperty(x.Name)!.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Length == 0)) ?? new List<INavigation>();
            if (navigations.Any() && parentProperties.Select(x => x.Value).Intersect(navigations.Select(x => x.Name)).Count() != navigations.Count())
            {
                foreach (INavigation navigationProperty in navigations)
                {
                    if (parentProperties.Any() && (depth == 0 || parentProperties.Count > depth)) parentProperties.Remove(parentProperties.Keys.Last()); //Clear out every time this loop backs all the way out to the original class
                    string navigationPropertyName = navigationProperty.Name;

                    //Prevent following circular references
                    if (!parentProperties.Select(x => x.Value).Contains(navigationPropertyName))
                    {
                        parentProperties.AddDictionaryItem(depth, navigationPropertyName);
                        query = query.IncludeNavigationProperties(context, navigationProperty.ClrType, depth + 1, maxDepth, parentProperties.DeepClone(), foundNavigations);
                    }
                }
            }
            else
            {
                //Reached the deepest navigation property
                string navigationString = string.Join(".", parentProperties.OrderBy(x => x.Key).Select(x => x.Value));
                query = query.Include(navigationString);
                foundNavigations.Add(navigationString, typeof(T));
            }
        }

        if (depth == 0 && useCaching && !cachedEntityNavigations.Any(x => x.Value == typeof(T)) && foundNavigations?.Any() == true)
        {
            foreach (KeyValuePair<string, Type> foundNavigation in foundNavigations)
            {
                cachedEntityNavigations.TryAdd(foundNavigation.Key, foundNavigation.Value);
            }
            //Only signal this is complete once all items have been added to avoid race conditions where cachedNavigations is accessed before all items have been added
            completeCachedEntities.AddDictionaryItem(typeof(T), true);
        }

        return query;
    }
}
