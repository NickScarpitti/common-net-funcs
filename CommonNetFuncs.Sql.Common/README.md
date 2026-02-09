# CommonNetFuncs.Sql.Common

[![License](https://img.shields.io/github/license/NickScarpitti/common-net-funcs.svg)](http://opensource.org/licenses/MIT)
[![NuGet Version](https://img.shields.io/nuget/v/CommonNetFuncs.Sql.Common)](https://www.nuget.org/packages/CommonNetFuncs.Sql.Common/)
[![nuget](https://img.shields.io/nuget/dt/CommonNetFuncs.Sql.Common)](https://www.nuget.org/packages/CommonNetFuncs.Sql.Common/)

This project contains helper methods for executing SQL queries and commands.

## Contents

- [CommonNetFuncs.Sql.Common](#commonnetfuncssqlcommon)
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
	- [QueryParameters](#queryparameters)
		- [QueryParameters Usage Examples](#queryparameters-usage-examples)
			- [CleanQueryParam](#cleanqueryparam)
			- [IsClean](#isclean)
			- [SanitizeSqlParameter](#sanitizesqlparameter)
	- [Installation](#installation)
	- [License](#license)

---

## DirectQuery

Helper methods for executing SQL queries and commands directly against a database.

### DirectQuery Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### GetDataTable

Executes a SELECT query asynchronously and returns the results as a DataTable.

```cs
await using SqliteCommand cmd = connection.CreateCommand();
cmd.CommandText = "SELECT * FROM TestTable";
using DataTable queryResultsTable = await DirectQuery.GetDataTable(connString, cmd); // queryResultsTable will contain the results of the query
```

#### GetDataTableSynchronous

Executes a SELECT query synchronously and returns the results as a DataTable.

```cs
using SqliteCommand cmd = connection.CreateCommand();
cmd.CommandText = "SELECT * FROM TestTable";
using DataTable queryResultsTable = DirectQuery.GetDataTable(connection, cmd); // queryResultsTable will contain the results of the query
```

#### RunUpdateQuery

Executes an UPDATE, INSERT, or DELETE query asynchronously and returns an UpdateResult containing the number of affected rows and a boolean indicating success.

```cs
await using SqliteCommand cmd = connection.CreateCommand();
cmd.CommandText = "UPDATE TestTable SET Name = 'Updated' WHERE Name LIKE 'Test%'";
UpdateResult updateResult = await DirectQuery.RunUpdateQuery(connection, cmd); // { RecordsChanged = 1, Success = true }
```

#### RunUpdateQuerySynchronous

Executes an UPDATE, INSERT, or DELETE query synchronously and returns an UpdateResult containing the number of affected rows and a boolean indicating success.

```cs
using SqliteCommand cmd = connection.CreateCommand();
cmd.CommandText = "UPDATE TestTable SET Name = 'Updated' WHERE Name LIKE 'Test%'";
UpdateResult updateResult = DirectQuery.RunUpdateQuery(connection, cmd); // { RecordsChanged = 1, Success = true }
```

#### GetDataStreamSynchronous

Gets a data from a query synchronously and returns an IEnumerable of the query result type.

```cs
using SqliteCommand cmd = connection.CreateCommand();
cmd.CommandText = "SELECT * FROM TestTable";
IEnumerable<TestEntity> queryResults = DirectQuery.GetDataStream(connection, cmd); // queryResults will contain the results of the query as TestEntity objects
```

#### GetDataStreamAsync

Gets a data from a query asynchronously and returns an IAsyncEnumerable of the query result type.

```cs
List<TestModel> results = new();
using SqliteCommand cmd = connection.CreateCommand();
cmd.CommandText = "SELECT * FROM TestTable";
await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd))
{
    results.Add(item); // Results will contain all items returned by the query
}
```

#### GetDataDirectAsync

Gets a data from a query asynchronously and returns an IEnumerable of the query result type.

```cs
using SqliteCommand cmd = connection.CreateCommand();
cmd.CommandText = "SELECT * FROM TestTable";
IEnumerable<TestEntity> queryResults = await DirectQuery.GetDataDirectAsync(connection, cmd); // queryResults will contain the results of the query as TestEntity objects
```

</details>

---

## QueryParameters

Helper class for cleaning query parameters. Please note that parameter cleaning is not a substitute for proper parameterization of SQL queries. This class is intended to be used in conjunction with parameterized queries to ensure that parameters are clean and safe.

### QueryParameters Usage Examples

<details>
<summary><h3>Usage Examples</h3></summary>

#### CleanQueryParam

Cleans potential issues out of a query parameter or query parameters, replacing standalone text "null" with null value or removing any new line characters and extra spaces

```cs
"\ntest\n ".CleanQueryParam(); // "test"
```

#### IsClean

Checks to make sure that a query parameter does not contain a set of potentially problematic characters / strings including but not limited to: `;` `'`, `[`, `]`, `"`, `/*`, `*/` `xp_`, `--`, and `

```cs
"text[".IsClean(); // false
```

#### SanitizeSqlParameter

Sanitizes a SQL parameter by escaping legal but potentially problematic characters or returns and empty string if rules are violated.

```cs
string result = "test'name".SanitizeSqlParameter(false, false, false, null, null); // "test''name"
string result = "test;name".SanitizeSqlParameter(false, false, false, null, null); // ""
```

</details>

## Installation

Install via NuGet:

```bash
dotnet add package CommonNetFuncs.Sql.Common
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/NickScarpitti/common-net-funcs/blob/main/LICENSE) file for details.
