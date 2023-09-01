using Common_Net_Funcs.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json;

namespace Common_Net_Funcs.EFCore;
public static class NavigationProperties
{
    public static IQueryable<T> IncludeNestedNavigationProperties<T>(this IQueryable<T> query, DbContext context, Type entityType, int depth = 0, int maxDepth = 100, Dictionary<int, string?>? parentProperties = null) where T : class
    {
        if (depth > maxDepth) return query;

        parentProperties ??= new();

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
                    query = query.IncludeNestedNavigationProperties(context, navigationProperty.ClrType, depth + 1, maxDepth, parentProperties.DeepClone());
                }
            }
        }
        else
        {
            //Reached the deepest navigation property
            query = query.Include(string.Join(".", parentProperties.OrderBy(x => x.Key).Select(x => x.Value)));
        }
        return query;
    }
}
