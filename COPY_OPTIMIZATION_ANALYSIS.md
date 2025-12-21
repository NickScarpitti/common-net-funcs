# Copy.cs Optimization Analysis

## Identified Optimization Opportunities

### 1. Redundant Property Array Creation

**Location**: `CopyPropertiesToNew<T, UT>` method (Line 154-158)
**Issue**: Properties are fetched even when useCache=true, but they're not used in the cached path.

```csharp
// Current code fetches these even when using cache:
IEnumerable<PropertyInfo> sourceProps = GetOrAddPropertiesFromReflectionCache(typeof(T)).Where(x => x.CanRead);
Dictionary<string, PropertyInfo> destPropDict = GetOrAddPropertiesFromReflectionCache(typeof(UT)).Where(x => x.CanWrite).ToDictionary(...);
```

**Fix**: Only fetch properties when useCache=false

### 2. ToArray() Overhead in Reflection Caching

**Location**: Multiple locations using `GetOrAddPropertiesFromReflectionCache(...).Where().ToArray()`
**Issue**: Creating intermediate arrays and LINQ overhead
**Fix**: Cache the filtered arrays directly or use spans/array segments

### 3. Repeated Type Checks in Collection Copying

**Location**: `CopyCollection` method
**Issue**: `IsSimpleType()` called multiple times for the same type in loops

```csharp
bool? itemIsSimpleType = null;  // Good pattern used
foreach (object? item in sourceCollection)
{
    itemIsSimpleType ??= item?.GetType().IsSimpleType();  // Cached per loop
}
```

**Improvement**: This pattern is already used but could be applied more consistently

### 4. Dictionary Creation Overhead

**Location**: Multiple locations creating `Dictionary<string, PropertyInfo>`
**Issue**: String comparisons with `StringComparer.Ordinal` + dictionary allocation
**Fix**: Consider using frozen collections or caching the dictionary

### 5. Expression Tree Compilation

**Location**: `CreatePropertyMappingsForCache<TSource, TDest>`
**Status**: ✅ Already using `CompileFast()` from FastExpressionCompiler - Good!

### 6. Cache Access Pattern

**Location**: `GetOrAddFunctionFromDeepCopyCache` and `GetOrAddFunctionFromCopyCache`
**Current**: Manual TryGetValue + conditional TryAdd
**Better**: Use GetOrAdd pattern consistently (already partially done)

### 7. Activator.CreateInstance Overhead

**Location**: Multiple places using `Activator.CreateInstance<T>()`
**Concern**: Reflection overhead for object creation
**Status**: Mitigated by caching in expression trees, but uncached paths still affected

## Priority Optimization Order

1. **High Priority**: Remove redundant property fetches in cached shallow copy (Easy win, frequently used)
2. **Medium Priority**: Optimize ToArray() calls and LINQ overhead
3. **Medium Priority**: Cache filtered property arrays
4. **Low Priority**: Frozen collections for property dictionaries (NET8+ feature)

## Benchmark Coverage

Created comprehensive benchmarks covering:

- ✅ Shallow copy (simple, complex, different types)
- ✅ Recursive copy (nested objects, collections)
- ✅ Collection copy (lists, dictionaries)
- ✅ Cached vs uncached paths
- ✅ CopyPropertiesTo variants

## Next Steps

1. Run baseline benchmarks ⏳ (In Progress)
2. Apply targeted optimizations
3. Re-run benchmarks
4. Compare results and validate improvements
