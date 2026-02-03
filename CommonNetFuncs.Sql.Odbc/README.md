# CommonNetFuncs.Sql.Odbc

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![Build](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml/badge.svg)](https://github.com/NickScarpitti/common-net-funcs/actions/workflows/dotnet.yml)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Sql.Odbc)](https://www.nuget.org/packages/CommonNetFuncs.Sql.Odbc/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Sql.Odbc)](https://www.nuget.org/packages/CommonNetFuncs.Sql.Odbc/)

This project contains helper methods for executing SQL queries and commands against an ODBC database.

## Contents

- [CommonNetFuncs.Sql.Odbc](#commonnetfuncssqlodbc)
  - [Contents](#contents)
  - [DirectQuery](#directquery)
    - [DirectQuery Usage Examples](#directquery-usage-examples)
      - [GetDataTable](#getdatatable)
      - [GetDataTableSynchronous](#getdatatablesynchronous)
      - [RunUpdateQuery](#runupdatequery)
      - [RunUpdateQuerySynchronous](#runupdatequerysynchronous)
      - [GetDataStreamSynchronous](#getdatastreamsynchronous)
      - [GetDataStreamAsync](#getdatastreamasync)
      - [GetDataDirectAsync](#getdatadirectasync)

---

## DirectQuery

Helper methods for executing SQL queries and commands directly against an ODBC database.

### DirectQuery Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetDataTable

Executes a SELECT query asynchronously and returns the results as a DataTable.

```cs
string sql = "SELECT * FROM TestTable";
using DataTable queryResultsTable = await DirectQuery.GetDataTable(sql, connectionString); // queryResultsTable will contain the results of the query
```

#### GetDataTableSynchronous

Executes a SELECT query synchronously and returns the results as a DataTable.

```cs
string sql = "SELECT * FROM TestTable";
using DataTable queryResultsTable = DirectQuery.GetDataTable(sql, connectionString); // queryResultsTable will contain the results of the query
```

#### RunUpdateQuery

Executes an UPDATE, INSERT, or DELETE query asynchronously and returns an UpdateResult containing the number of affected rows and a boolean indicating success.

```cs
string sql = "UPDATE TestTable SET Name = 'Updated' WHERE Name LIKE 'Test%'";
UpdateResult updateResult = await DirectQuery.RunUpdateQuery(sql, connectionString); // { RecordsChanged = 1, Success = true }
```

#### RunUpdateQuerySynchronous

Executes an UPDATE, INSERT, or DELETE query synchronously and returns an UpdateResult containing the number of affected rows and a boolean indicating success.

```cs
string sql = "UPDATE TestTable SET Name = 'Updated' WHERE Name LIKE 'Test%'";
UpdateResult updateResult = DirectQuery.RunUpdateQuerySynchronous(sql, connectionString); // { RecordsChanged = 1, Success = true }
```

#### GetDataStreamSynchronous

Gets a data from a query synchronously and returns an IEnumerable of the query result type.

```cs
string sql = "SELECT * FROM TestTable";
IEnumerable<TestEntity> queryResults = DirectQuery.GetDataStreamSynchronous(sql, connectionString); // queryResults will contain the results of the query as TestEntity objects
```

#### GetDataStreamAsync

Gets a data from a query asynchronously and returns an IAsyncEnumerable of the query result type.

```cs
List<TestModel> results = new();
string sql = "SELECT * FROM TestTable";
await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(sql, connectionString))
{
    results.Add(item); // Results will contain all items returned by the query
}
```

#### GetDataDirectAsync

Gets a data from a query asynchronously and returns an IEnumerable of the query result type.

```cs
string sql = "SELECT * FROM TestTable";
IEnumerable<TestEntity> queryResults = await DirectQuery.GetDataDirectAsync(sql, connectionString); // queryResults will contain the results of the query as TestEntity objects
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Sql.Odbc
```

## Dependencies

- CommonNetFuncs.Core
- CommonNetFuncs.Sql.Common
- System.Data.Odbc (>= 9.0.1)

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.