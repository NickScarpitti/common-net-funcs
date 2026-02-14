# Circular Reference Testing Notes

## Summary

Added test infrastructure and entity models to support testing of circular reference handling in `BaseDbContextActions.Helpers.cs`.

## What Was Added

### 1. Test Entities (TestContext.cs)

- `ParentEntity`: Entity with a collection of children
- `ChildEntity`: Entity with a parent reference
- `CircularRefDbContext`: DbContext configured with bidirectional relationship between Parent and Child entities

### 2. Test Infrastructure

- `CreateCircularRefServiceProvider()`: Helper method that creates a service provider configured with `CircularRefDbContext`
- Uses in-memory database provider for testing
- Configured with `AddDbContextPool` to match the pattern used in other tests

### 3. Tests Created

- `CircularRefContext_DirectQuery_ShouldWork`: ✅ **PASSING** - Validates that the circular reference entities and DbContext work correctly
- `CircularRefEntities_BasicCRUD_ShouldSucceed`: Tests GetAll and GetByKey with circular reference entities
- `CircularRefEntities_FilterAndPaging_ShouldSucceed`: Tests filtering and paging operations
- `CircularRefEntities_SelectProjection_ShouldSucceed`: Tests projection queries
- `CircularRefEntities_BidirectionalNavigation_ShouldSucceed`: Tests navigation properties

## Current Status

The diagnostic test (`CircularRefContext_DirectQuery_ShouldWork`) **passes successfully**, confirming that:

- Entities with bidirectional relationships can be created and saved
- The DbContext configuration is correct
- EF Core handles the circular references properly

### Known Issues

The tests using `BaseDbContextActions<TEntity, TContext>` are currently failing with null results. This appears to be an integration issue where:

1. `BaseDbContextActions` retrieves the `DbContext` from the service provider
2. The retrieved context instance may not have visibility to the data saved by the test setup
3. This could be related to context pooling, scope management, or entity configuration

###Key Code Locations

- Circular reference handling logic: [BaseDbContextActions.Helpers.cs](../CommonNetFuncs.EFCore/BaseDbContextActions.Helpers.cs) lines 124-184
  - `ExecuteWithCircularRefHandling<TResult>`: Line 124
  - `ExecuteStreamingWithCircularRefHandling<T>`: Line 157
- Test entities: [TestContext.cs](TestContext.cs) lines 54-96
- Test methods: [BaseDbContextActionsTests.Helpers.cs](BaseDbContextActionsTests.Helpers.cs) lines 242-426

## Circular Reference Logic Being Tested

The helper methods in `BaseDbContextActions.Helpers.cs` implement special handling for circular reference scenarios:

```csharp
// When InvalidOperationException (HResult -2146233079) is caught:
// 1. Adds entity to circularReferencingEntities dictionary
// 2. Retries the query with handlingCircularRef=true
// 3. On subsequent queries, checks dictionary and uses AsNoTracking if entity has circular refs
```

This logic helps avoid infinite loops and performance issues when querying entities with bidirectional navigation properties.

## Next Steps for Full Coverage

To achieve complete test coverage of the circular reference handling code:

1. **Investigate BaseDbContextActions Integration**: Determine why queries return null when using the custom CircularRefDbContext
   - Check if entity type registration is the issue
   - Verify context scope and lifecycle management
   - Consider using actual database provider (SQLite file-based) instead of in-memory

2. **Alternative Approach**: Consider adding the circular reference entities to the main `TestDbContext` instead of creating a separate context

3. **Direct Testing**: Create tests that directly call the`ExecuteWithCircularRefHandling` methods if they can be made accessible for testing

## References

- Original request: Cover missing code in [BaseDbContextActions.Helpers.cs](../CommonNetFuncs.EFCore/BaseDbContextActions.Helpers.cs)
- User emphasis: "This is a critical code path to test, since circular references are not uncommon in a lot of EF Core usecases"
