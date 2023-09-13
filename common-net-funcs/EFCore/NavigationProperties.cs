using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;
using static Common_Net_Funcs.Tools.DataValidation;
using static Common_Net_Funcs.Tools.DeepCloneExpressionTreeHelpers;

namespace Common_Net_Funcs.EFCore;

public static class NavigationProperties
{
    //Navigations are cached by type to prevent having to discover them every time they are needed
    static readonly ConcurrentDictionary<Type, List<string>> cachedEntityNavigations = new();

    public static IQueryable<T> IncludeNavigationProperties<T>(this IQueryable<T> query, DbContext context, Type entityType) where T : class
    {
        foreach (string navigation in GetNavigations<T>(context, entityType) ?? new())
        {
            query = query.Include(navigation);
        }
        return query;
    }

    public static List<string>? GetNavigations<T>(DbContext context, Type entityType, int depth = 0, int maxDepth = 100, List<string>? topLevelProperties = null,
        Dictionary<int, string?>? parentProperties = null, Dictionary<string, Type>? foundNavigations = null, bool useCaching = true) where T : class
    {
        if (depth > maxDepth) return null;

        parentProperties ??= new();
        foundNavigations ??= new();
        topLevelProperties ??= new();

        if (depth == 0)
        {
            if (useCaching && cachedEntityNavigations.Any(x => x.Key == typeof(T))) //&& navigations.Count() <= cachedEntityNavigations.Count(x => x.Value == typeof(T)))
            {
                return cachedEntityNavigations[typeof(T)];
            }
        }

        IEnumerable<INavigation> navigations = (context.Model.FindEntityType(entityType)?.GetNavigations()
            .Where(x => entityType.GetProperty(x.Name)!.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Length == 0)) ?? new List<INavigation>();

        if (depth == 0)
        {
            topLevelProperties = navigations.Select(x => x.Name).ToList();
        }

        if (navigations.Any() && parentProperties.Select(x => x.Value).Intersect(navigations.Select(x => x.Name)).Count() != navigations.Count())
        {
            foreach (INavigation navigationProperty in navigations)
            {
                if (parentProperties.Any() && (depth == 0 || parentProperties.Count > depth)) parentProperties.Remove(parentProperties.Keys.Last()); //Clear out every time this loop backs all the way out to the original class
                string navigationPropertyName = navigationProperty.Name;

                //Prevent following circular references or redundant branches
                if (!parentProperties.Select(x => x.Value).Contains(navigationPropertyName)) //&& (depth == 0 || !topLevelProperties.Any(x => x == navigationPropertyName)))
                {
                    parentProperties.AddDictionaryItem(depth, navigationPropertyName);
                    //query = query.IncludeNavigationProperties(context, navigationProperty.ClrType, depth + 1, maxDepth, topLevelProperties, parentProperties.DeepClone(), foundNavigations);

                    //No need to keep reassigning the query as nothing is changing through each iteration
                    //query.IncludeNavigationProperties(context, navigationProperty.ClrType, depth + 1, maxDepth, topLevelProperties, parentProperties.DeepClone(), foundNavigations);
                    foundNavigations!.AddDictionaryItem(string.Join(".", parentProperties.OrderBy(x => x.Key).Select(x => x.Value)), typeof(T)); //Ensure that every step is called out in case the end navigation is null to ensure prior values are loaded
                    GetNavigations<T>(context, !navigationProperty.ClrType.GenericTypeArguments.Any() ? navigationProperty.ClrType : navigationProperty.ClrType.GenericTypeArguments[0],
                        depth + 1, maxDepth, topLevelProperties, parentProperties.DeepClone(), foundNavigations);
                }
            }
        }
        else
        {
            //Reached the deepest navigation property
            //query = query.Include(navigationString); //Will add all navigations at the end once they're all found
            foundNavigations!.AddDictionaryItem(string.Join(".", parentProperties.OrderBy(x => x.Key).Select(x => x.Value)), typeof(T));
        }

        if (depth == 0 && useCaching && !cachedEntityNavigations.Any(x => x.Key == typeof(T)) && foundNavigations?.Any() == true)
        {
            cachedEntityNavigations.TryAdd(typeof(T), foundNavigations.Select(x => x.Key).ToList());
        }
        return foundNavigations?.Select(x => x.Key).ToList();
    }

    public static List<string> GetTopLevelNavigations(DbContext context, Type entityType)
    {
        List<string> topLevelNavigations = cachedEntityNavigations.Where(x => x.Key == entityType).SelectMany(x => x.Value).Where(x => !x.Contains('.')).ToList();
        if (!topLevelNavigations.Any())
        {
            IEnumerable<INavigation> navigations = (context.Model.FindEntityType(entityType)?.GetNavigations()
                .Where(x => entityType.GetProperty(x.Name)!.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Length == 0)) ?? new List<INavigation>();
            topLevelNavigations = navigations.Select(x => x.Name).ToList();
        }
        return topLevelNavigations;
    }

    public static void RemoveNavigationProperties<T>(this T obj, DbContext context) where T : class
    {
        foreach (PropertyInfo prop in obj.GetType().GetProperties().Where(x => GetTopLevelNavigations(context, typeof(T)).Contains(x.Name)))
        {
            prop.SetValue(obj, null);
        }
    }
}
