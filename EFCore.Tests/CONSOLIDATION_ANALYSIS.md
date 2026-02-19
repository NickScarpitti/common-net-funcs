# BaseDbContextActionsTests Consolidation Analysis

## Summary

**Initial Count**: 308 tests (263 Facts + 45 Theories)
**Current Count**: 304 tests (259 Facts + 45 Theories)
**Tests Eliminated**: 4 (from Helpers.cs)
**Target Potential**: ~100-120 tests could be eliminated through systematic consolidation

## Completed Consolidations

### Helpers.cs (Eliminated 4 tests)

1. **FullQueryOptions Tests** (Eliminated 2):
   - Removed `Constructor_DefaultValues_SetsPropertiesToNull` - merged into Theory
   - Removed `FullQueryOptions_CanBeUsedInCollections` - redundant test

2. **GenericPagingModel Tests** (Eliminated 2):
   - Removed `Constructor_Default_InitializesEmptyList` - merged assertion into existing Theory
   - Removed `Entities_SetAndGet_WorksCorrectly` - basic property test, unnecessary

## Major Consolidation Opportunities

### 1. Write.cs (48 tests) - Potential to eliminate ~15-20 tests

#### A. Create/CreateMany Operations (7 tests → 1-2 theories)

**Pattern**: Testing Create/CreateMany with/without removeNavigationProps

- `Create_WithValidEntity_ShouldAddToDatabase`
- `CreateMany_ShouldAddEntities`
- `Create_WithNullEntity_ShouldThrow`
- `Create_WithRemoveNavigationProps_ShouldNotThrow`
- `CreateMany_WithRemoveNavigationProps_ShouldNotThrow`
- `CreateMany_WithValidEntities_ShouldSucceed`
- `CreateMany_WithNavigationProps_ShouldRemoveNavigationProps`

**Consolidate To**:

```csharp
[Theory]
[InlineData(CrudOperation.Create, NavigationPropertyHandling.Keep, false)]
[InlineData(CrudOperation.Create, NavigationPropertyHandling.Remove, false)]
[InlineData(CrudOperation.CreateMany, NavigationPropertyHandling.Keep, false)]
[InlineData(CrudOperation.CreateMany, NavigationPropertyHandling.Remove, false)]
public async Task CreateOperations_WithVariations_ShouldWork(CrudOperation operation, NavigationPropertyHandling navPropHandling, bool expectThrow)
```

#### B. Update/UpdateMany Operations (11 tests → 2-3 theories)

**Pattern**: Similar to Create - testing with removeNavigationProps, globalFilterOptions, exceptions

**Groups**:

1. Basic Update/UpdateMany (5 tests)
2. Update with GlobalFilterOptions (3 tests)
3. Exception handling (3 tests)

#### C. Delete Operations (27 tests → 8-10 theories)

**Major Pattern Groups**:

1. **DeleteByKey variations** (4 tests): valid/invalid keys, globalFilterOptions
2. **DeleteByObject variations** (4 tests): removeNavigationProps, globalFilterOptions
3. **DeleteMany variations** (7 tests): entities list, removeNavigationProps, globalFilterOptions
4. **DeleteMany with Expression** (4 tests): ExecuteDeleteAsync scenarios
5. **Exception handling** (3 tests)
6. **Advanced scenarios** (5 tests): tracked entities, compound keys

### 2. Read.cs (238 tests) - Potential to eliminate ~60-80 tests

#### A. GetByKey Tests (9 tests → 2-3 theories)

**Pattern**: Testing full/not full, compound keys, tracking

- Multiple `GetByKey_*` tests
- Multiple `GetByKeyFull_*` tests

**Consolidate To**:

```csharp
[Theory]
[InlineData(QueryType.Standard, false)] // GetByKey without tracking
[InlineData(QueryType.Standard, true)]  // GetByKey with tracking
[InlineData(QueryType.Full, false)]     // GetByKeyFull without tracking
[InlineData(QueryType.Full, true)]      // GetByKeyFull with tracking
public async Task GetByKey_WithVariations_ReturnsEntity(QueryType queryType, bool trackEntities)
```

#### B. GetAll Tests (~30 tests → 8-10 theories)

**Major Variations**:

1. **Full/Not Full + Tracking**: Already has some Theory coverage, but many separate Facts exist
2. **GlobalFilterOptions variations** (~10 tests): DisableAllFilters, FilterNamesToDisable, empty arrays, null values
3. **Projection variations** (~5 tests): with/without projection
4. **Streaming variations** (~10 tests): GetAllStreaming, GetAllFullStreaming with various options

**Recommended Consolidation**:

```csharp
[Theory]
[InlineData(QueryType.Standard, TrackingBehavior.NoTracking, GlobalFilterMode.None, ProjectionMode.Entity)]
[InlineData(QueryType.Full, TrackingBehavior.WithTracking, GlobalFilterMode.DisableAll, ProjectionMode.Projection)]
// ... more combinations
public async Task GetAll_WithVariations_ReturnsEntities(...)
```

#### C. GetWithFilter Tests (~40 tests → 10-12 theories)

**Massive duplication** - similar patterns to GetAll:

1. Full/Not Full variations (already some Theory coverage)
2. GlobalFilterOptions (many separate Facts)
3. SplitQueryOverride (true/false/null) - many Facts testing each
4. Projection variations
5. Streaming vs non-streaming
6. Tracking behavior

**Example Consolidation**:

```csharp
[Theory]
[InlineData(QueryType.Standard, ExecutionMode.Synchronous, ProjectionMode.Entity)]
[InlineData(QueryType.Full, ExecutionMode.Streaming, ProjectionMode.Projection)]
// ... more combinations
public async Task GetWithFilter_WithVariations_ReturnsFilteredEntities(...)
```

#### D. GetOneWithFilter Tests (~15 tests → 4-5 theories)

Similar patterns to GetWithFilter but for single entity retrieval

#### E. GetMaxByOrder/GetMinByOrder Tests (~16 tests → 4-5 theories)

**Pattern**: Testing full/not full, SplitQueryOverride, tracking, globalFilterOptions

- Many duplicate test structures between Max and Min

**Consolidate To**:

```csharp
[Theory]
[InlineData(OrderOperation.Max, QueryType.Standard, TrackingBehavior.NoTracking)]
[InlineData(OrderOperation.Min, QueryType.Full, TrackingBehavior.WithTracking)]
public async Task GetByOrder_WithVariations_ReturnsOrderedEntity(...)
```

#### F. GetMax/GetMin Tests (~16 tests → 4-5 theories)

Similar to GetMaxByOrder/GetMinByOrder but returns scalar values

#### G. GetNavigationWithFilter Tests (~12 tests → 3-4 theories)

**Pattern**: Full/not full, streaming, SplitQueryOverride, globalFilterOptions

#### H. GlobalFilterOptions Redundant Tests (~20+ tests)

**Major Issue**: ~20+ tests explicitly testing GlobalFilterOptions variations:

- DisableAllFilters = true/false
- FilterNamesToDisable with various arrays (null, empty, with values)
- These are tested repeatedly across EVERY query method

**Consolidation Strategy**: Create ONE comprehensive GlobalFilterOptions theory that tests the filter behavior infrastructure, then remove the redundant globalFilterOptions tests from individual methods

```csharp
[Theory]
[InlineData(GlobalFilterMode.None, null)]
[InlineData(GlobalFilterMode.DisableAll, null)]
[InlineData(GlobalFilterMode.DisableSpecific, "Filter1,Filter2")]
public async Task GlobalFilterOptions_AppliedToQueries_WorksCorrectly(GlobalFilterMode mode, string? filterNames)
```

#### I. SplitQueryOverride Redundant Tests (~15+ tests)

Similar to GlobalFilterOptions - tested repeatedly across methods

- true/false/null variations tested separately for many methods
- Can consolidate into fewer comprehensive tests

## Implementation Strategy

### Phase 1: Quick Wins (Eliminate ~20 tests)

1. ✅ Consolidate FullQueryOptions tests in Helpers.cs
2. ✅ Consolidate GenericPagingModel tests in Helpers.cs
3. Consolidate GetByKey full/not full Facts into existing Theories
4. Consolidate GetMax/GetMin full/not full Facts into existing Theories
5. Remove redundant globalFilterOptions null/empty array tests

### Phase 2: Medium Impact (Eliminate ~40 tests)

1. Consolidate Create/CreateMany operations in Write.cs
2. Consolidate Update/UpdateMany operations in Write.cs
3. Consolidate Delete operation groups in Write.cs
4. Consolidate GetOneWithFilter full/not full variations
5. Consolidate GetMaxByOrder/GetMinByOrder variations

### Phase 3: Large Refactoring (Eliminate ~40-50 tests)

1. Create comprehensive GlobalFilterOptions test theory
2. Remove redundant globalFilterOptions tests from all query methods
3. Create comprehensive SplitQueryOverride test theory
4. Remove redundant splitQueryOverride tests from query methods
5. Consolidate GetAll test variations
6. Consolidate GetWithFilter test variations
7. Consolidate streaming vs non-streaming duplicate patterns

## Enums Created for Consolidation

```csharp
public enum CrudOperation { Create, CreateMany, Update, UpdateMany, DeleteByKey, DeleteByObject, DeleteMany }
public enum QueryType { Standard, Full }
public enum NavigationPropertyHandling { Keep, Remove }
public enum TrackingBehavior { NoTracking, WithTracking }
public enum SplitQueryOption { Null, False, True }
public enum GlobalFilterMode { None, DisableAll, DisableSpecific }
public enum ExecutionMode { Synchronous, Streaming }
public enum ProjectionMode { Entity, Projection }
```

## Tests to Keep As-Is (Do NOT Consolidate)

### Circular Reference Handling Tests (Helpers.cs)

These test complex internal implementation details and edge cases:

- `GetAllFull_WithCircularReferences_ShouldHandleNavigationProperties`
- `GetAllFullStreaming_WithCircularReferences_ShouldStreamResults`
- `CircularReferenceHandling_MultipleCallsSameEntity_ShouldUseCachedBehavior`
- `CircularReferenceHandling_StreamingMultipleCalls_ShouldUseCachedBehavior`
- `CircularReferenceHandling_CheckDictionaryState_ShouldCacheEntityType`
- `ExecuteWithCircularRefHandling_*` tests (5 tests)
- `ExecuteStreamingWithCircularRefHandling_*` tests (2 tests)

**Reason**: These test specific exception handling paths, dictionary caching behavior, and edge cases in the circular reference handling logic. Each tests a unique scenario.

### Exception Handling Tests

- Tests that verify specific exception types and error scenarios
- Tests using mocked/fake contexts to trigger exceptions
- Tests for DbUpdateException handling

**Reason**: Each tests a different error path

### Database-specific Tests

- Tests using SQLite for realistic ExecuteUpdateAsync/ExecuteDeleteAsync behavior
- Tests that verify database provider-specific behavior

**Reason**: These test integration with real database providers

## Estimated Final Count

**Optimistic**: ~190-200 tests (eliminate ~100-110 tests)
**Realistic**: ~220-230 tests (eliminate ~75-85 tests)
**Conservative**: ~250-260 tests (eliminate ~45-55 tests)

## Risks & Considerations

1. **Test Readability**: Over-consolidation can make tests harder to understand
2. **Failure Diagnosis**: Consolidated tests with many InlineData values can be harder to debug
3. **Test Isolation**: Some tests may have subtle differences that make consolidation risky
4. **Time Investment**: Full consolidation would require 8-16 hours of careful work
5. **Regression Risk**: Must verify all consolidated logic is maintained

## Recommendations

1. **Implement Phase 1 now** (~2 hours, low risk, high ROI)
2. **Implement Phase 2 gradually** (~4-6 hours, medium risk, good ROI)
3. **Phase 3 requires careful planning** (~8-12 hours, higher risk, but highest impact)
4. **Run full test suite after each consolidation**
5. **Consider doing Phase 3 in multiple PRs to minimize risk**
