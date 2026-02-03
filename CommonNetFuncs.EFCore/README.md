# CommonNetFuncs.EFCore

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![Build](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.EFCore)](https://www.nuget.org/packages/CommonNetFuncs.EFCore/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.EFCore)](https://www.nuget.org/packages/CommonNetFuncs.EFCore/)

This project contains helper methods for several common Entity Framework Core operations.

## Contents

- [CommonNetFuncs.EFCore](#commonnetfuncsefcore)
  - [Contents](#contents)
  - [BaseDbContextActions](#basedbcontextactions)
    - [BaseDbContextActions Usage Examples](#basedbcontextactions-usage-examples)
      - [GetByKey](#getbykey)
      - [GetAll](#getall)
      - [GetWithFilter](#getwithfilter)
      - [GetNavigationWithFilter](#getnavigationwithfilter)
    - [GetWithPagingFilter](#getwithpagingfilter)
      - [GetOneWithFilter](#getonewithfilter)
      - [GetMaxByOrder](#getmaxbyorder)
      - [GetMinByOrder](#getminbyorder)
      - [GetMax](#getmax)
      - [GetMin](#getmin)
      - [GetCount](#getcount)
      - [Create](#create)
      - [CreateMany](#createmany)
      - [Update](#update)
      - [UpdateMany](#updatemany)
      - [DeleteByObject](#deletebyobject)
      - [DeleteByKey](#deletebykey)
      - [DeleteMany](#deletemany)
      - [SaveChanges](#savechanges)
  - [NavigationProperties](#navigationproperties)
    - [NavigationProperties Usage Examples](#navigationproperties-usage-examples)
      - [IncludeNavigationProperties](#includenavigationproperties)
      - [GetNavigations](#getnavigations)
      - [GetTopLevelNavigations](#gettoplevelnavigations)
      - [RemoveNavigationProperties](#removenavigationproperties)

---

## BaseDbContextActions

Provides a set of generic helper methods for querying and manipulating entities in an EF Core `DbContext`. It supports single and composite keys, full graph loading (navigation properties), streaming, projections, filtering, paging, and basic CRUD operations. The class is designed to simplify common data access patterns and reduce boilerplate code. The "Full" parameter / methods indicate loading the full object graph for an entity.

### BaseDbContextActions Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetByKey

Retrieves a single entity by its primary key. Supports both full (with navigation properties) and simple queries.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

// Retrieve an entity by its primary key
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
TestEntity? entity = await actions.GetByKey(full: false, primaryKey: 1);
```

#### GetAll

Retrieves all entities from the database. Can optionally include navigation properties and control entity tracking.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

// Retrieve all entities, optionally including navigation properties and tracking
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
List<TestEntity>? entities = await actions.GetAll(full: true, trackEntities: false); // Gets all entities without tracking them
```

#### GetWithFilter

Retrieves entities matching a specified filter expression. Supports full graph loading and projections.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

// Retrieve entities matching a filter
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
List<TestEntity>? filtered = await actions.GetWithFilter(full: false, whereExpression: x => x.Name == "Target"); // Returns entities where Name == "Target"
```

#### GetNavigationWithFilter

Retrieves entities matching a specified filter expression. Supports full graph loading and projections.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

public class TestEntityDetail
{
    public int Id { get; set; }
    public required string Description { get; set; }
    public int TestEntityId { get; set; }

    [JsonIgnore]
    public TestEntity? TestEntity { get; set; }
}

// Retrieve entities matching a filter
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);

Expression<Func<TestEntityDetail, bool>> where = x => x.TestEntityId == 1;
Expression<Func<TestEntityDetail, TestEntity>> select = x => x.TestEntity!;

List<TestEntity>? filtered = await actions.GetNavigationWithFilter(full: false, where, select); // Returns test entity via navigation property on TestEntityDetail
```

### GetWithPagingFilter

Gets entities matching a specified filter expression by pages with the specified skip and page size parameters.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

public sealed class GenericPagingModel<T> where T : class
{
    public GenericPagingModel()
    {
        Entities = [];
    }
    public List<T> Entities { get; set; }
    public int TotalRecords { get; set; }
}

GenericPagingModel<TestEntity> result = await testContext.GetWithPagingFilter(whereExpression: _ => true, selectExpression: x => x, orderByString: nameof(TestEntity.Id), skip: 1, pageSize: 2); // Skips first match and takes 2nd and 3rd record ordered by Id
```

#### GetOneWithFilter

Retrieves a single entity matching a filter expression. Returns null if not found.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

// Retrieve a single entity by filter
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
TestEntity? entity = await actions.GetOneWithFilter(x => x.Id == 1); // Returns first entity where Id == 1
```

#### GetMaxByOrder

Retrieves the entity with the maximum value for a specified property, optionally filtered.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

// Retrieve the entity with the maximum Id
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
TestEntity? maxEntity = await actions.GetMaxByOrder(full: false, whereExpression: _ => true, descendingOrderExpression: x => x.Id);
```

#### GetMinByOrder

Retrieves the entity with the minimum value for a specified property, optionally filtered.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

// Retrieve the entity with the minimum Id
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
TestEntity? minEntity = await actions.GetMinByOrder(_ => true, x => x.Id);
```

#### GetMax

Returns the maximum value of a specified property for entities matching a filter.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

// Get the maximum Id value
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
int maxId = await actions.GetMax(_ => true, x => x.Id);
```

#### GetMin

Returns the minimum value of a specified property for entities matching a filter.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

// Get the minimum Id value
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
int minId = await actions.GetMin(_ => true, x => x.Id);
```

#### GetCount

Returns the count of entities matching a filter.

```cs
// Count all entities
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
int count = await actions.GetCount(_ => true);
```

#### Create

Adds a new entity to the context.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

// Add a new entity
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
await actions.Create(new TestEntity { Name = "New" });
await actions.SaveChanges();
```

#### CreateMany

Adds multiple new entities to the context.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

// Add multiple entities
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
await actions.CreateMany(new List<TestEntity> { new() { Name = "A" }, new() { Name = "B" } });
await actions.SaveChanges();
```

#### Update

Updates an existing entity in the context.

```cs
public class TestEntity
{
    public int Id { get; set; } // Primary key
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public ICollection<TestEntityDetail>? Details { get; set; }
}

// Update an entity
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider); entity.Name = "Updated";
actions.Update(entity);
await actions.SaveChanges();
```

#### UpdateMany

Updates multiple entities in the context.

```cs
// Update multiple entities
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
actions.UpdateMany(entities); // Entities here is multiple changed entities
await actions.SaveChanges();
```

#### DeleteByObject

Removes an entity from the context.

```cs
// Delete an entity by object
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
actions.DeleteByObject(entity);
await actions.SaveChanges();
```

#### DeleteByKey

Removes an entity by its primary key.

```cs
// Delete an entity by key
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
bool deleted = await actions.DeleteByKey(entity.Id);
await actions.SaveChanges();
```

#### DeleteMany

Removes multiple entities from the context.

```cs
// Delete multiple entities
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
actions.DeleteMany(entities);
await actions.SaveChanges();
```

#### SaveChanges

Commits all changes made in the context to the database.

```cs
// Save changes to the database
BaseDbContextActions<TestEntity, TestDbContext> actions = new(serviceProvider);
bool success = await actions.SaveChanges();
```

</details>

---

## NavigationProperties

[Description here]

### NavigationProperties Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### IncludeNavigationProperties

[Method Description here]

```cs
//Code example here
```

#### GetNavigations

[Method Description here]

```cs
//Code example here
```

#### GetTopLevelNavigations

[Method Description here]

```cs
//Code example here
```

#### RemoveNavigationProperties

[Method Description here]

```cs
//Code example here
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.EFCore
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.

