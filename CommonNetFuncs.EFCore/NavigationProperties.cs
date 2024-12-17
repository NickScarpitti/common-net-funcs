using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using CommonNetFuncs.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;
using static CommonNetFuncs.Core.Collections;

namespace CommonNetFuncs.EFCore;

public static class NavigationProperties
{
    //Navigations are cached by type to prevent having to discover them every time they are needed
    //static readonly ConcurrentDictionary<Type, List<string>> cachedEntityNavigations = new();

    ///// <summary>
    ///// Adds navigation properties onto an EF Core query.
    ///// </summary>
    ///// <typeparam name="T">The entity to use as the starting point for getting navigation properties.</typeparam>
    ///// <param name="query">IQueryable representing the EF core query.</param>
    ///// <param name="context">The DBContext being queried against.</param>
    ///// <param name="maxDepth">The maximum number of navigations deep to follow. Default is 100 which should be more than enough to get all navigations in most scenarios.</param>
    ///// <returns>IQueryable object with include statements for its navigation properties.</returns>
    //public static IQueryable<T> IncludeNavigationProperties<T>(this IQueryable<T> query, DbContext context, int maxDepth = 100) where T : class
    //{
    //    foreach (string? navigation in GetNavigations<T>(context, typeof(T), maxDepth: maxDepth) ?? [])
    //    {
    //        if (!navigation.IsNullOrWhiteSpace())
    //        {
    //            query = query.Include(navigation);
    //        }
    //    }
    //    return query;
    //}

    ///// <summary>
    ///// Gets all of the navigations of entity T as a list of string through recursive iterations through each navigation property.
    ///// </summary>
    ///// <typeparam name="T">The entity to use as the starting point for getting navigation properties.</typeparam>
    ///// <param name="context">The context that contains the definition for entity T.</param>
    ///// <param name="entityType">The type of the current navigation property. Should always start as typeof(T).</param>
    ///// <param name="depth">Zero based depth of the current navigation property relative to the entity T.</param>
    ///// <param name="maxDepth">The maximum number of navigations deep to follow. Default is 100 which should be more than enough to get all navigations in most scenarios.</param>
    ///// <param name="topLevelNavigations">The navigation properties of entity T.</param>
    ///// <param name="parentNavigations">A dictionary of each navigation property mapping from the top level navigation to the current depth navigation property.
    ///// If there is ever a duplicate property discovered in this chain, the recursive loop backs out from that point to prevent infinite loops.</param>
    ///// <param name="foundNavigations">A dictionary holding all of the navigations to be used as Include statements in a EF Core query.</param>
    ///// <param name="useCaching">If true, store the results of the GetNavigations operation by entity type so they can be looked up on subsequent calls instead of repeating the discovery process.</param>
    ///// <returns>A list of string containing all of the navigations of entity T that can be directly used as Include statements in an EF Core query.</returns>
    //public static List<string>? GetNavigations<T>(DbContext context, Type entityType, int depth = 0, int maxDepth = 100, List<string>? topLevelNavigations = null,
    //    Dictionary<int, string?>? parentNavigations = null, Dictionary<string, Type>? foundNavigations = null, bool useCaching = true) where T : class
    //{
    //    if (depth > maxDepth)
    //    {
    //        return null;
    //    }

    //    parentNavigations ??= [];
    //    foundNavigations ??= [];
    //    topLevelNavigations ??= [];

    //    if (depth == 0)
    //    {
    //        if (useCaching && cachedEntityNavigations.Any(x => x.Key == typeof(T))) //&& navigations.Count() <= cachedEntityNavigations.Count(x => x.Value == typeof(T)))
    //        {
    //            return cachedEntityNavigations[typeof(T)];
    //        }
    //    }

    //    IEnumerable<INavigation> navigations = (context.Model.FindEntityType(entityType)?.GetNavigations()
    //        .Where(x => entityType.GetProperty(x.Name)!.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), true).Length == 0 &&
    //            entityType.GetProperty(x.Name)!.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Length == 0)) ?? [];

    //    if (depth == 0)
    //    {
    //        topLevelNavigations = navigations.Select(x => x.Name).ToList();
    //    }

    //    if (navigations.Any() && parentNavigations.Select(x => x.Value).Intersect(navigations.Select(x => x.Name)).Count() != navigations.Count())
    //    {
    //        foreach (INavigation navigationProperty in navigations)
    //        {
    //            if (parentNavigations.AnyFast() && (depth == 0 || parentNavigations.Count > depth))
    //            {
    //                parentNavigations.Remove(parentNavigations.Keys.Last()); //Clear out every time this loop backs all the way out to the original class
    //            }

    //            string navigationPropertyName = navigationProperty.Name;

    //            //Prevent following circular references or redundant branches
    //            if (!parentNavigations.Select(x => x.Value).Contains(navigationPropertyName)) //&& (depth == 0 || !topLevelProperties.Any(x => x == navigationPropertyName)))
    //            {
    //                parentNavigations.TryAdd(depth, navigationPropertyName);

    //                //No need to keep reassigning the query as nothing is changing through each iteration
    //                foundNavigations!.TryAdd(string.Join(".", parentNavigations.OrderBy(x => x.Key).Select(x => x.Value)), typeof(T)); //Ensure that every step is called out in case the end navigation is null to ensure prior values are loaded
    //                GetNavigations<T>(context, !navigationProperty.ClrType.GenericTypeArguments.AnyFast() ? navigationProperty.ClrType : navigationProperty.ClrType.GenericTypeArguments[0],
    //                    depth + 1, maxDepth, topLevelNavigations, parentNavigations.DeepClone(), foundNavigations);
    //            }
    //        }
    //    }
    //    else
    //    {
    //        //Reached the deepest navigation property
    //        foundNavigations!.TryAdd(string.Join(".", parentNavigations.OrderBy(x => x.Key).Select(x => x.Value)), typeof(T));
    //    }

    //    if (depth == 0 && useCaching && !cachedEntityNavigations.Any(x => x.Key == typeof(T)) && foundNavigations?.AnyFast() == true)
    //    {
    //        cachedEntityNavigations.TryAdd(typeof(T), foundNavigations.Select(x => x.Key).ToList());
    //    }
    //    return foundNavigations?.Select(x => x.Key).ToList();
    //}

    ///// <summary>
    ///// Sets all navigation properties in the provided entity to null.
    ///// </summary>
    ///// <typeparam name="T">The entity type to remove the navigation properties from.</typeparam>
    ///// <param name="obj">The object of type T to remove the navigation properties from.</param>
    ///// <param name="context">The context that contains the definition for entity T.</param>
    //public static void RemoveNavigationProperties<T>(this T obj, DbContext context) where T : class
    //{
    //    foreach (PropertyInfo prop in obj.GetType().GetProperties().Where(x => GetTopLevelNavigations<T>(context).Contains(x.Name)))
    //    {
    //        prop.SetValue(obj, null);
    //    }
    //}

    /// <summary>
    /// Adds navigation properties onto an EF Core query.
    /// </summary>
    /// <typeparam name="T">The entity to use as the starting point for getting navigation properties.</typeparam>
    /// <param name="query">IQueryable representing the EF core query.</param>
    /// <param name="context">The DBContext being queried against.</param>
    /// <param name="maxDepth">The maximum number of navigations deep to follow. Default is 100 which should be more than enough to get all navigations in most scenarios.</param>
    /// <returns>IQueryable object with include statements for its navigation properties.</returns>
    public static IQueryable<T> IncludeNavigationProperties<T>(this IQueryable<T> query, DbContext context, int maxDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T : class
    {
        HashSet<string> navigations = GetNavigations<T>(context, maxDepth, navPropAttributesToIgnore, useCaching);
        return navigations.Aggregate(query, (current, path) => current.Include(path));
    }

    private readonly record struct NavigationNode(string Name, Type Type);
    private static readonly ConcurrentDictionary<(Type Type, string? NavigationPropertyTypesToIgnore), HashSet<string>> CachedEntityNavigations = new();

    /// <summary>
    /// Gets all of the navigations of entity T as a list of string through recursive iterations through each navigation property.
    /// </summary>
    /// <typeparam name="T">The entity to use as the starting point for getting navigation properties.</typeparam>
    /// <param name="context">The context that contains the definition for entity T.</param>
    /// <param name="maxDepth">The maximum number of navigations deep to follow. Default is 100 which should be more than enough to get all navigations in most scenarios.</param>
    /// <param name="navPropAttributesToIgnore">Optional: The attribute types used to ignore class properties when navigating through the object tree. If null, uses System.Text.Json.Serialization.JsonIgnoreAttribute and Newtonsoft.Json.JsonIgnoreAttribute</param>
    /// <param name="useCaching">If true, store the results of the GetNavigations operation by entity type so they can be looked up on subsequent calls instead of repeating the discovery process.</param>
    /// <returns>A HashSet of strings containing all of the navigations of entity T that can be directly used as Include statements in an EF Core query.</returns>
    public static HashSet<string> GetNavigations<T>(DbContext context, int maxDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T : class
    {
        // Try get from cache first
        if (useCaching && CachedEntityNavigations.TryGetValue((typeof(T), navPropAttributesToIgnore.CreateNavPropsIgnoreString()), out HashSet<string>? cachedPaths))
        {
            return cachedPaths;
        }

        HashSet<NavigationNode> visitedNode = [];
        HashSet<string> paths = [];

        void TraverseNavigations(Type entityType, Stack<string> currentPath, int depth)
        {
            if (depth > maxDepth) return;

            IEntityType? entityTypeInfo = context.Model.FindEntityType(entityType);
            if (entityTypeInfo == null) return;

            foreach (INavigation navigation in entityTypeInfo.GetNavigations())
            {
                // Skip if property has JsonIgnore attribute
                PropertyInfo? propertyInfo = entityType.GetProperty(navigation.Name);
                if (navPropAttributesToIgnore != null)
                {
                    if (navPropAttributesToIgnore.Any(x => propertyInfo?.GetCustomAttributes(x, true).AnyFast() == true))
                    {
                        continue;
                    }
                }
                else if (propertyInfo?.GetCustomAttributes<JsonIgnoreAttribute>(true).Any() == true || propertyInfo?.GetCustomAttributes<System.Text.Json.Serialization.JsonIgnoreAttribute>(true).Any() == true)
                {
                    continue;
                }

                Type targetType = navigation.ClrType.IsGenericType ? navigation.ClrType.GenericTypeArguments[0] : navigation.ClrType;

                NavigationNode node = new(navigation.Name, targetType);

                // Check for circular reference using both name and type
                if (!visitedNode.Add(node)) { continue; }

                currentPath.Push(navigation.Name);
                paths.Add(string.Join(".", currentPath.Reverse())); //Reverse order since Stack is LIFO

                TraverseNavigations(targetType, currentPath, depth + 1);

                currentPath.Pop();
                visitedNode.Remove(node);
            }
        }

        TraverseNavigations(typeof(T), new(), 0);

        // Cache the results
        CachedEntityNavigations.TryAdd((typeof(T), navPropAttributesToIgnore.CreateNavPropsIgnoreString()), paths);

        return paths;
    }

    /// <summary>
    /// Get the names of the classes representing the navigation properties in entity T.
    /// </summary>
    /// <typeparam name="T">The entity type to get the navigation properties of.</typeparam>
    /// <param name="context">The context that contains the definition for entity T.</param>
    /// <param name="navPropAttributesToIgnore">Optional: The attribute types used to ignore top level class properties. If null, uses System.Text.Json.Serialization.JsonIgnoreAttribute and Newtonsoft.Json.JsonIgnoreAttribute</param>
    /// <returns>List of string representing the names of all of the navigation properties in entity T.</returns>
    public static List<string> GetTopLevelNavigations<T>(DbContext context, List<Type>? navPropAttributesToIgnore = null)
    {
        Type entityClassType = typeof(T);
        List<string> topLevelNavigations = CachedEntityNavigations.Where(x => x.Key == (entityClassType, navPropAttributesToIgnore.CreateNavPropsIgnoreString()))
            .SelectMany(x => x.Value).Where(x => !x.Contains('.')).ToList(); //Remove any with a '.' since these are deeper than top level
        if (!topLevelNavigations.AnyFast())
        {
            //IEnumerable<INavigation> navigations = (context.Model.FindEntityType(entityType)?.GetNavigations()
            //    .Where(x => entityType.GetProperty(x.Name)!.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), true).Length == 0 &&
            //        entityType.GetProperty(x.Name)!.GetCustomAttributes(typeof(JsonIgnoreAttribute), true).Length == 0)) ?? [];

            IEntityType? entityType = context.Model.FindEntityType(entityClassType);
            IEnumerable<INavigation> navigations = navPropAttributesToIgnore != null ?
                entityType?.GetNavigations().Where(x => !navPropAttributesToIgnore.Any(y => entityClassType.GetProperty(x.Name)!.GetCustomAttributes(y, true).AnyFast())) ?? [] :
                (entityType?.GetNavigations().Where(x => !entityClassType.GetProperty(x.Name)!.GetCustomAttributes<System.Text.Json.Serialization.JsonIgnoreAttribute>(true).Any() &&
                    !entityClassType.GetProperty(x.Name)!.GetCustomAttributes<JsonIgnoreAttribute>(true).Any())) ?? [];

            topLevelNavigations = navigations.Select(x => x.Name).ToList();
        }
        return topLevelNavigations;
    }

    private static string? CreateNavPropsIgnoreString(this List<Type>? navPropAttributesToIgnore)
    {
        return navPropAttributesToIgnore != null ? string.Join("|", navPropAttributesToIgnore.OrderBy(x => x.Name)) : null;
    }

    private static readonly ConcurrentDictionary<Type, Action<object>> _navigationSetterCache = new();

    /// <summary>
    /// Sets all navigation properties in the provided entity to null.
    /// </summary>
    /// <typeparam name="T">The entity type to remove the navigation properties from.</typeparam>
    /// <param name="obj">The object of type T to remove the navigation properties from.</param>
    /// <param name="context">The context that contains the definition for entity T.</param>
    public static void RemoveNavigationProperties<T>(this T obj, DbContext context) where T : class
    {
        if (obj == null) return;

        Action<T> setter = _navigationSetterCache.GetOrAdd(typeof(T), type =>
        {
            // Get navigation property names
            List<string> navigations = GetTopLevelNavigations<T>(context);

            // Parameter for the entity instance
            ParameterExpression parameter = Expression.Parameter(typeof(object), "entity");
            UnaryExpression convertedParameter = Expression.Convert(parameter, type);

            // Create assignments for each navigation property
            List<BinaryExpression?> assignments = navigations.Select(navProp =>
            {
                PropertyInfo? property = type.GetProperty(navProp);
                if (property?.CanWrite != true)
                {
                    return null;
                }

                return Expression.Assign(Expression.Property(convertedParameter, property), Expression.Constant(null, property.PropertyType));
            })
            .Where(assignment => assignment != null)
            .ToList();

            // If no valid assignments, return empty action
            if (!assignments.AnyFast())
            {
                return new Action<object>(_ => { });
            }

            // Create a block with all assignments
            BlockExpression block = Expression.Block(assignments.SelectNonNull());

            // Compile the expression tree into a delegate
            return Expression.Lambda<Action<object>>(block, parameter).Compile();
        });

        // Execute the cached setter
        setter(obj);
    }
}
