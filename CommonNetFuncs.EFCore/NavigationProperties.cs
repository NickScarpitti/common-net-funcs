using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using CommonNetFuncs.Core;
using FastExpressionCompiler;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;
using static CommonNetFuncs.Core.ReflectionCaches;
using static CommonNetFuncs.Core.Strings;

namespace CommonNetFuncs.EFCore;

/// <summary>
/// Optional configurations for methods in the NavigationProperties class.
/// </summary>
public class NavigationPropertiesOptions(int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
{
    /// <summary>
    /// <para>Optional: Set the 0 based maximum depth of non-looping navigation properties to be included in the query. Only used when running "full" queries that include navigation properties.</para>
    /// <para>Values less than 1 are considered no limit on maximum depth</para>
    /// </summary>
    public int MaxNavigationDepth { get; set; } = maxNavigationDepth;

    /// <summary>
    /// Optional: Attributes to ignore when including navigation properties in the query. Only used when running "full" queries that include navigation properties.
    /// </summary>
    public List<Type>? NavPropAttributesToIgnore { get; set; } = navPropAttributesToIgnore;

    /// <summary>
    /// Optional: Cache the navigation properties for the return query class. Only used when running "full" queries that include navigation properties.
    /// </summary>
    public bool UseCaching { get; set; } = useCaching;
}

public static class NavigationProperties
{
    #region Caching

    public readonly struct NavigationProperiesCacheKey(Type sourceType, string? navigationPropertyTypesToIgnore) : IEquatable<NavigationProperiesCacheKey>
    {
        public readonly Type SourceType = sourceType;
        public readonly string? NavigationPropertyTypesToIgnore = navigationPropertyTypesToIgnore;

        public bool Equals(NavigationProperiesCacheKey other)
        {
            return other.SourceType == SourceType && other.NavigationPropertyTypesToIgnore == NavigationPropertyTypesToIgnore;
        }

        public override bool Equals(object? obj)
        {
            return obj is NavigationProperiesCacheKey navigationProperiesCacheKey && Equals(navigationProperiesCacheKey);
        }

        public override int GetHashCode()
        {
            return SourceType.GetHashCode() + NavigationPropertyTypesToIgnore?.GetHashCode() ?? 0;
        }

        public static bool operator ==(NavigationProperiesCacheKey left, NavigationProperiesCacheKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NavigationProperiesCacheKey left, NavigationProperiesCacheKey right)
        {
            return !(left == right);
        }
    }

    public readonly struct NavigationProperiesCacheValue(HashSet<string> navigationProperties, int maxDepth) : IEquatable<NavigationProperiesCacheValue>
    {
        public readonly HashSet<string> NavigationProperties = navigationProperties;
        public readonly int MaxDepth = maxDepth;

        public bool Equals(NavigationProperiesCacheValue other)
        {
            return other.MaxDepth == MaxDepth && other.NavigationProperties.SetEquals(NavigationProperties);
        }

        public override bool Equals(object? obj)
        {
            return obj is NavigationProperiesCacheValue navigationProperiesCacheKey && Equals(navigationProperiesCacheKey);
        }

        public override int GetHashCode()
        {
            return NavigationProperties.GetHashCode() + MaxDepth.GetHashCode();
        }

        public HashSet<string> GetNavigationsToDepth(int depth)
        {
            if (depth < 0)
            {
                return NavigationProperties;
            }
            return NavigationProperties.Where(x => x.HasNoMoreThanNumberOfChars('.', depth)).ToHashSet();
        }

        public static bool operator ==(NavigationProperiesCacheValue left, NavigationProperiesCacheValue right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NavigationProperiesCacheValue left, NavigationProperiesCacheValue right)
        {
            return !(left == right);
        }
    }

    private static readonly CacheManager<NavigationProperiesCacheKey, NavigationProperiesCacheValue> NavigationCache = new();

    public static ICacheManagerApi<NavigationProperiesCacheKey, NavigationProperiesCacheValue> NavigationCacheManager => NavigationCache;

    private static readonly CacheManager<Type, List<string>> TopLevelNavigationCache = new();

    public static ICacheManagerApi<Type, List<string>> TopLevelNavigationCacheManager => TopLevelNavigationCache;

    /// <summary>
    /// Clears LimitedEntityNavigationsCache cache and sets the size to the specified value.
    /// </summary>
    /// <param name="key">Type to get properties for. Will store </param>
    private static HashSet<string> GetOrAddPropertiesFromEntityNavigationsCache<T>(NavigationProperiesCacheKey key, DbContext context, NavigationPropertiesOptions navigationPropertiesOptions) where T : class
    {
        if (NavigationCacheManager.IsUsingLimitedCache() ?
            NavigationCacheManager.GetLimitedCache().TryGetValue(new(typeof(T), navigationPropertiesOptions.NavPropAttributesToIgnore.CreateNavPropsIgnoreString()), out NavigationProperiesCacheValue cachedValue) :
            NavigationCacheManager.GetCache().TryGetValue(new(typeof(T), navigationPropertiesOptions.NavPropAttributesToIgnore.CreateNavPropsIgnoreString()), out cachedValue))
        {
            if (cachedValue.MaxDepth < 0 || cachedValue.MaxDepth >= navigationPropertiesOptions.MaxNavigationDepth) //Cached value to requested depth exists
            {
                return cachedValue.GetNavigationsToDepth(navigationPropertiesOptions.MaxNavigationDepth);
            }
        }

        HashSet<string> navigations = GetNewNavigations<T>(context, navigationPropertiesOptions);
        if (NavigationCacheManager.IsUsingLimitedCache())
        {
            NavigationCacheManager.TryAddLimitedCache(key, new(navigations, navigationPropertiesOptions.MaxNavigationDepth));
        }
        else
        {
            NavigationCacheManager.TryAddCache(key, new(navigations, navigationPropertiesOptions.MaxNavigationDepth));
        }
        return navigations;
    }

    /// <summary>
    /// Clears LimitedTopLevelNavigationsCache cache and sets the size to the specified value.
    /// </summary>
    /// <param name="type">Type to get properties for. Will store </param>
    private static List<string> GetOrAddPropertiesFromTopLevelNavigationsCache(Type type, DbContext context, List<Type>? navPropAttributesToIgnore = null)
    {
        if (TopLevelNavigationCacheManager.IsUsingLimitedCache() ? TopLevelNavigationCacheManager.GetLimitedCache().TryGetValue(type, out List<string>? cachedNavigations) : TopLevelNavigationCacheManager.GetCache().TryGetValue(type, out cachedNavigations))
        {
            return cachedNavigations ?? [];
        }

        List<string> navigations = GetNewTopLevelNavigations(type, context, navPropAttributesToIgnore);
        if (TopLevelNavigationCacheManager.IsUsingLimitedCache())
        {
            TopLevelNavigationCacheManager.TryAddLimitedCache(type, navigations);
        }
        else
        {
            TopLevelNavigationCacheManager.TryAddCache(type, navigations);
        }
        return navigations;
    }

    #endregion

    /// <summary>
    /// Adds navigation properties onto an EF Core query.
    /// </summary>
    /// <typeparam name="T">The entity to use as the starting point for getting navigation properties.</typeparam>
    /// <param name="query">IQueryable representing the EF core query.</param>
    /// <param name="context">The DBContext being queried against.</param>
    /// <param name="navigationPropertiesOptions">Used to configure optional parameters for getting navigation properties</param>
    /// <returns>IQueryable object with include statements for its navigation properties.</returns>
    public static IQueryable<T> IncludeNavigationProperties<T>(this IQueryable<T> query, DbContext context, NavigationPropertiesOptions? navigationPropertiesOptions = null) where T : class
    {
        navigationPropertiesOptions ??= new NavigationPropertiesOptions();
        HashSet<string> navigations = GetNavigations<T>(context, navigationPropertiesOptions);
        return navigations.Aggregate(query, (current, path) => current.Include(path));
    }

    private readonly record struct NavigationNode(string Name, Type Type);

    /// <summary>
    /// Gets all of the navigations of entity T as a list of string through recursive iterations through each navigation property.
    /// </summary>
    /// <typeparam name="T">The entity to use as the starting point for getting navigation properties.</typeparam>
    /// <param name="context">The context that contains the definition for entity T.</param>
    /// <param name="navigationPropertiesOptions">Used to configure optional parameters for getting navigation properties</param>
    /// <returns>A HashSet of strings containing all of the navigations of entity T that can be directly used as Include statements in an EF Core query.</returns>
    public static HashSet<string> GetNavigations<T>(DbContext context, NavigationPropertiesOptions? navigationPropertiesOptions = null) where T : class
    {
        navigationPropertiesOptions ??= new();

        return navigationPropertiesOptions.UseCaching ?
            GetOrAddPropertiesFromEntityNavigationsCache<T>(new(typeof(T), navigationPropertiesOptions.NavPropAttributesToIgnore.CreateNavPropsIgnoreString()), context, navigationPropertiesOptions) :
            GetNewNavigations<T>(context, navigationPropertiesOptions);
    }

    private static HashSet<string> GetNewNavigations<T>(DbContext context, NavigationPropertiesOptions navigationPropertiesOptions) where T : class
    {
        HashSet<NavigationNode> visitedNode = [];
        HashSet<string> paths = [];

        void TraverseNavigations(Type entityType, Stack<string> currentPath, int depth)
        {
            if (depth > navigationPropertiesOptions.MaxNavigationDepth)
            {
                return;
            }

            IEntityType? entityTypeInfo = context.Model.FindEntityType(entityType);
            if (entityTypeInfo == null)
            {
                return;
            }

            foreach (INavigation navigation in entityTypeInfo.GetNavigations())
            {
                // Skip if property has JsonIgnore attribute
                PropertyInfo? propertyInfo = GetOrAddPropertiesFromReflectionCache(entityType).First(x => x.Name.StrComp(navigation.Name)); //entityType.GetProperty(navigation.Name);
                if (navigationPropertiesOptions.NavPropAttributesToIgnore != null)
                {
                    bool skipProperty = false;
                    for (int i = 0; i < navigationPropertiesOptions.NavPropAttributesToIgnore.Count; i++)
                    {
                        if (propertyInfo?.GetCustomAttributes(navigationPropertiesOptions.NavPropAttributesToIgnore[i], true).AnyFast() == true)
                        {
                            skipProperty = true;
                            break;
                        }
                    }

                    if (skipProperty)
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
                if (!visitedNode.Add(node))
                {
                    continue;
                }

                currentPath.Push(navigation.Name);
                StringBuilder stringBuilder = new();

                foreach (string pathSegment in currentPath.Reverse()) //Reverse order since Stack is LIFO
                {
                    stringBuilder.Append(pathSegment).Append('.');
                }
                stringBuilder.Length--; // Remove the last '.'
                paths.Add(stringBuilder.ToString());

                TraverseNavigations(targetType, currentPath, depth + 1);

                currentPath.Pop();
                visitedNode.Remove(node);
            }
        }

        TraverseNavigations(typeof(T), new(), 0);

        return paths;
    }

    /// <summary>
    /// Get the names of the classes representing the navigation properties in entity T.
    /// </summary>
    /// <typeparam name="T">The entity type to get the navigation properties of.</typeparam>
    /// <param name="context">The context that contains the definition for entity T.</param>
    /// <param name="navPropAttributesToIgnore">Optional: The attribute types used to ignore top level class properties. If null, uses System.Text.Json.Serialization.JsonIgnoreAttribute and Newtonsoft.Json.JsonIgnoreAttribute</param>
    /// <returns><see cref="List{T}"/> of string representing the names of all of the navigation properties in entity T.</returns>
    public static List<string> GetTopLevelNavigations<T>(DbContext context, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        Type entityClassType = typeof(T);

        return useCaching ? GetOrAddPropertiesFromTopLevelNavigationsCache(entityClassType, context, navPropAttributesToIgnore) :
            GetNewTopLevelNavigations(entityClassType, context, navPropAttributesToIgnore);
    }

    private static List<string> GetNewTopLevelNavigations(Type entityClassType, DbContext context, List<Type>? navPropAttributesToIgnore = null)
    {
        List<string> topLevelNavigations = NavigationCacheManager.IsUsingLimitedCache() ? NavigationCacheManager.GetLimitedCache().Where(x => x.Key.Equals(new(entityClassType, navPropAttributesToIgnore.CreateNavPropsIgnoreString())))
             .SelectMany(x => x.Value.NavigationProperties).Where(x => !x.Contains('.')).ToList() : //Remove any with a '.' since these are deeper than top level
             NavigationCacheManager.GetCache().Where(x => x.Key.Equals(new(entityClassType, navPropAttributesToIgnore.CreateNavPropsIgnoreString())))
             .SelectMany(x => x.Value.NavigationProperties).Where(x => !x.Contains('.')).ToList(); //Remove any with a '.' since these are deeper than top level

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

    private static readonly ConcurrentDictionary<Type, Action<object>> NavigationSetterCache = new();

    /// <summary>
    /// Sets all navigation properties in the provided entity to null.
    /// </summary>
    /// <typeparam name="T">The entity type to remove the navigation properties from.</typeparam>
    /// <param name="obj">The object of type T to remove the navigation properties from.</param>
    /// <param name="context">The context that contains the definition for entity T.</param>
    public static void RemoveNavigationProperties<T>(this T obj, DbContext context) where T : class
    {
        if (obj == null)
        {
            return;
        }

        Action<T> setter = NavigationSetterCache.GetOrAdd(typeof(T), type =>
        {
            // Get navigation property names
            List<string> navigations = GetTopLevelNavigations<T>(context);

            // Parameter for the entity instance
            ParameterExpression parameter = Expression.Parameter(typeof(object), "entity");
            UnaryExpression convertedParameter = Expression.Convert(parameter, type);

            // Create assignments for each navigation property
            List<BinaryExpression> assignments = [];
            foreach (string navProp in navigations)
            {
                PropertyInfo? property = type.GetProperty(navProp);
                if (property?.CanWrite == true)
                {
                    assignments.Add(Expression.Assign(Expression.Property(convertedParameter, property), Expression.Constant(null, property.PropertyType)));
                }
            }

            // If no valid assignments, return empty action
            if (!assignments.AnyFast())
            {
                return new Action<object>(_ => { });
            }

            // Create a block with all assignments
            BlockExpression block = Expression.Block(assignments);

            // Compile the expression tree into a delegate
            return Expression.Lambda<Action<object>>(block, parameter).CompileFast();
        });

        // Execute the cached setter
        setter(obj);
    }
}
