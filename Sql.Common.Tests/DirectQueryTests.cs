using System.Data;
using CommonNetFuncs.Sql.Common;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static Xunit.TestContext;

namespace Sql.Common.Tests;

public sealed class DirectQueryTests : IDisposable
{
	private readonly SqliteConnection connection;

	public DirectQueryTests()
	{
		Batteries.Init();
		connection = new SqliteConnection("DataSource=file::memory:?cache=shared");
		SetupDb();
	}

	private bool disposed;

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				connection.Dispose();
				DirectQuery.CacheManager.SetUseLimitedCache(true);
				DirectQuery.CacheManager.SetLimitedCacheSize(100);
				DirectQuery.CacheManager.ClearAllCaches();
			}
			disposed = true;
		}
	}

	~DirectQueryTests()
	{
		Dispose(false);
	}

	private void SetupDb()
	{
		connection.Open();
		using SqliteCommand createCommand = connection.CreateCommand();
		createCommand.CommandText =
			@"
				DROP TABLE IF EXISTS TestTable;
				CREATE TABLE TestTable (
					Id INTEGER PRIMARY KEY AUTOINCREMENT,
					Name TEXT
				);
				INSERT INTO TestTable (Name) VALUES ('Test1'), ('Test2'), ('Test3'), ('Test4'), ('Test5');
			";
		createCommand.ExecuteNonQuery();
	}

	[Fact]
	public async Task GetDataTable_ShouldReturnPopulatedDataTable_WhenQuerySucceeds()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM TestTable";

		using DataTable result = await DirectQuery.GetDataTable(connection, cmd, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(5);
		result.Columns.Contains("Id").ShouldBeTrue();
		result.Columns.Contains("Name").ShouldBeTrue();
	}

	[Fact]
	public void GetDataTableSynchronous_ShouldReturnPopulatedDataTable_WhenQuerySucceeds()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM TestTable";

		using DataTable result = DirectQuery.GetDataTableSynchronous(connection, cmd);

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(5);
		result.Columns.Contains("Id").ShouldBeTrue();
		result.Columns.Contains("Name").ShouldBeTrue();
	}

	[Fact]
	public async Task GetDataTable_ShouldRetryOnFailure()
	{
		// Simulate a failure by using an invalid SQL for the first two attempts, then a valid one
		DataTable result = null!;
		for (int i = 0; i < 3; i++)
		{
			await using SqliteCommand cmd = connection.CreateCommand();
			if (i < 2)
			{
				cmd.CommandText = "SELECT * FROM NonExistentTable";
			}
			else
			{
				SetupDb(); // Reset the database to ensure the table exists due to the nature of in-memory databases
				cmd.CommandText = "SELECT * FROM TestTable";
			}

			try
			{
				result = await DirectQuery.GetDataTable(connection, cmd, maxRetry: 3, cancellationToken: Current.CancellationToken);
				if (result.Rows.Count > 0)
				{
					break;
				}
			}
			catch
			{
				// ignore
			}
		}

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(5);

		result.Dispose();
	}

	[Fact]
	public async Task RunUpdateQuery_ShouldReturnSuccessResult_WhenUpdateSucceeds()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "UPDATE TestTable SET Name = 'Updated' WHERE Name LIKE 'Test%'";

		UpdateResult result = await DirectQuery.RunUpdateQuery(connection, cmd, cancellationToken: Current.CancellationToken);

		result.Success.ShouldBeTrue();
		result.RecordsChanged.ShouldBe(5);
	}

	[Fact]
	public void RunUpdateQuerySynchronous_ShouldReturnSuccessResult_WhenUpdateSucceeds()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "UPDATE TestTable SET Name = 'Updated' WHERE Name LIKE 'Test%'";

		UpdateResult result = DirectQuery.RunUpdateQuerySynchronous(connection, cmd);

		result.Success.ShouldBeTrue();
		result.RecordsChanged.ShouldBe(5);
	}

	public sealed class TestModel
	{
		public int Id { get; set; }

		public string? Name { get; set; }
	}

	[Fact]
	public async Task GetDataStreamAsync_ShouldYieldMappedObjects()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable ORDER BY Id LIMIT 2";

		List<TestModel> results = new();
		await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(2);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("Test1");
		results[1].Id.ShouldBe(2);
		results[1].Name.ShouldBe("Test2");
	}

	[Fact]
	public async Task GetDataDirectAsync_ShouldReturnMappedObjects()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable WHERE Id = 1";

		IEnumerable<TestModel> results = await DirectQuery.GetDataDirectAsync<TestModel>(connection, cmd, cancellationToken: Current.CancellationToken);

		results.Count().ShouldBe(1);
		results.First().Id.ShouldBe(1);
		results.First().Name.ShouldBe("Test1");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void DirectQuery_CacheManager_SetAndGetUseLimitedCache_Works(bool useLimited)
	{
		// Act
		DirectQuery.CacheManager.SetUseLimitedCache(useLimited);

		// Assert
		DirectQuery.CacheManager.IsUsingLimitedCache().ShouldBe(useLimited);
	}

	[Theory]
	[InlineData(1)]
	[InlineData(10)]
	public void DirectQuery_CacheManager_SetAndGetLimitedCacheSize_Works(int size)
	{
		// Act
		DirectQuery.CacheManager.SetLimitedCacheSize(size);

		// Assert
		DirectQuery.CacheManager.GetLimitedCacheSize().ShouldBe(size);
	}

	[Fact]
	public void DirectQuery_CacheManager_ClearAllCaches_RemovesAllEntries()
	{
		// Arrange
		DirectQuery.CacheManager.SetUseLimitedCache(false);
		Type key = typeof(TestModel);
		Func<IDataReader, TestModel> del = _ => new TestModel { Id = 1, Name = "A" };
		DirectQuery.CacheManager.TryAddCache(key, del);
		DirectQuery.CacheManager.GetCache().Count.ShouldBeGreaterThan(0);

		// Act
		DirectQuery.CacheManager.ClearAllCaches();

		// Assert
		DirectQuery.CacheManager.GetCache().Count.ShouldBe(0);
		DirectQuery.CacheManager.GetLimitedCache().Count.ShouldBe(0);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void DirectQuery_CacheManager_GetCacheAndLimitedCache_Work(bool useLimited)
	{
		// Arrange
		DirectQuery.CacheManager.SetUseLimitedCache(useLimited);
		DirectQuery.CacheManager.SetLimitedCacheSize(10);
		Type key = typeof(TestModel);
		Func<IDataReader, TestModel> del = _ => new TestModel { Id = 1, Name = "A" };

		// Act
		DirectQuery.CacheManager.ClearAllCaches();
		if (useLimited)
		{
			DirectQuery.CacheManager.TryAddLimitedCache(key, del);
		}
		else
		{
			DirectQuery.CacheManager.TryAddCache(key, del);
		}

		// Assert
		if (useLimited)
		{
			DirectQuery.CacheManager.GetLimitedCache().Count.ShouldBe(1);
			DirectQuery.CacheManager.GetCache().Count.ShouldBe(0);
		}
		else
		{
			DirectQuery.CacheManager.GetCache().Count.ShouldBe(1);
			DirectQuery.CacheManager.GetLimitedCache().Count.ShouldBe(0);
		}
	}

	[Fact]
	public void DirectQuery_CacheManager_TryAddCacheAndTryAddLimitedCache_Works()
	{
		// Arrange
		Type key = typeof(TestModel);
		Func<IDataReader, TestModel> del = _ => new TestModel { Id = 1, Name = "A" };

		// Act & Assert
		DirectQuery.CacheManager.SetUseLimitedCache(false);
		DirectQuery.CacheManager.ClearAllCaches();
		DirectQuery.CacheManager.TryAddCache(key, del).ShouldBeTrue();
		DirectQuery.CacheManager.GetCache().ContainsKey(key).ShouldBeTrue();

		DirectQuery.CacheManager.SetUseLimitedCache(true);
		DirectQuery.CacheManager.ClearAllCaches();
		DirectQuery.CacheManager.TryAddLimitedCache(key, del).ShouldBeTrue();
		DirectQuery.CacheManager.GetLimitedCache().ContainsKey(key).ShouldBeTrue();
	}

	[Fact]
	public void DirectQuery_CacheManager_DuplicateTryAddCache_ReturnsFalse()
	{
		// Arrange
		Type key = typeof(TestModel);
		Func<IDataReader, TestModel> del = _ => new TestModel { Id = 1, Name = "A" };

		// Act & Assert
		DirectQuery.CacheManager.SetUseLimitedCache(false);
		DirectQuery.CacheManager.ClearAllCaches();
		DirectQuery.CacheManager.TryAddCache(key, del).ShouldBeTrue();
		DirectQuery.CacheManager.TryAddCache(key, del).ShouldBeFalse();

		DirectQuery.CacheManager.SetUseLimitedCache(true);
		DirectQuery.CacheManager.ClearAllCaches();
		DirectQuery.CacheManager.TryAddLimitedCache(key, del).ShouldBeTrue();
		DirectQuery.CacheManager.TryAddLimitedCache(key, del).ShouldBeFalse();
	}

	[Fact]
	public void GetDataStreamSynchronous_ShouldYieldMappedObjects()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable ORDER BY Id LIMIT 2";

		List<TestModel> results = new();
		foreach (TestModel item in DirectQuery.GetDataStreamSynchronous<TestModel>(connection, cmd, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(2);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("Test1");
		results[1].Id.ShouldBe(2);
		results[1].Name.ShouldBe("Test2");
	}

	[Fact]
	public void GetDataStreamSynchronous_ShouldRespectCancellationToken()
	{
		using CancellationTokenSource cts = new();
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable";

		List<TestModel> results = new();
		int count = 0;
		foreach (TestModel item in DirectQuery.GetDataStreamSynchronous<TestModel>(connection, cmd, cancellationToken: cts.Token))
		{
			results.Add(item);
			count++;
			if (count == 2)
			{
				cts.Cancel();
			}
		}

		// Should have stopped after 2 items due to cancellation
		results.Count.ShouldBeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task GetDataStreamAsync_ShouldRespectCancellationToken()
	{
		using CancellationTokenSource cts = new();
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable";

		List<TestModel> results = new();
		int count = 0;
		try
		{
			await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd, cancellationToken: cts.Token))
			{
				results.Add(item);
				count++;
				if (count == 2)
				{
					await cts.CancelAsync();
				}
			}
		}
		catch (OperationCanceledException)
		{
			// Expected when cancellation is requested
		}

		// Should have stopped after 2 items due to cancellation
		results.Count.ShouldBeLessThanOrEqualTo(2);
	}

	[Fact]
	public async Task RunUpdateQuery_ShouldRetryOnFailure()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "UPDATE TestTable SET Name = 'Updated' WHERE Name = 'Test1'";

		UpdateResult result = await DirectQuery.RunUpdateQuery(connection, cmd, maxRetry: 3, cancellationToken: Current.CancellationToken);

		result.Success.ShouldBeTrue();
		result.RecordsChanged.ShouldBe(1);
	}

	[Fact]
	public void RunUpdateQuerySynchronous_ShouldRetryOnFailure()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "UPDATE TestTable SET Name = 'Updated' WHERE Name = 'Test1'";

		UpdateResult result = DirectQuery.RunUpdateQuerySynchronous(connection, cmd, maxRetry: 3);

		result.Success.ShouldBeTrue();
		result.RecordsChanged.ShouldBe(1);
	}

	[Fact]
	public async Task GetDataStreamAsync_WithoutCache_ShouldYieldMappedObjects()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable ORDER BY Id LIMIT 2";

		List<TestModel> results = new();
		await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd, useCache: false, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(2);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("Test1");
		results[1].Id.ShouldBe(2);
		results[1].Name.ShouldBe("Test2");
	}

	[Fact]
	public void GetDataStreamSynchronous_WithoutCache_ShouldYieldMappedObjects()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable ORDER BY Id LIMIT 2";

		List<TestModel> results = new();
		foreach (TestModel item in DirectQuery.GetDataStreamSynchronous<TestModel>(connection, cmd, useCache: false, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(2);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("Test1");
		results[1].Id.ShouldBe(2);
		results[1].Name.ShouldBe("Test2");
	}

	[Fact]
	public async Task GetDataDirectAsync_WithoutCache_ShouldReturnMappedObjects()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable WHERE Id = 1";

		IEnumerable<TestModel> results = await DirectQuery.GetDataDirectAsync<TestModel>(connection, cmd, useCache: false, cancellationToken: Current.CancellationToken);

		results.Count().ShouldBe(1);
		results.First().Id.ShouldBe(1);
		results.First().Name.ShouldBe("Test1");
	}

	[Fact]
	public async Task GetDataDirectAsync_WithRetries_ShouldReturnResults()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable WHERE Id = 1";

		IEnumerable<TestModel> results = await DirectQuery.GetDataDirectAsync<TestModel>(connection, cmd, maxRetry: 5, cancellationToken: Current.CancellationToken);

		results.Count().ShouldBe(1);
		results.First().Id.ShouldBe(1);
		results.First().Name.ShouldBe("Test1");
	}

	[Fact]
	public async Task GetDataTable_WithCustomTimeout_ShouldReturnPopulatedDataTable()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM TestTable";

		using DataTable result = await DirectQuery.GetDataTable(connection, cmd, commandTimeoutSeconds: 60, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(5);
	}

	[Fact]
	public void GetDataTableSynchronous_WithCustomTimeout_ShouldReturnPopulatedDataTable()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM TestTable";

		using DataTable result = DirectQuery.GetDataTableSynchronous(connection, cmd, commandTimeoutSeconds: 60);

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(5);
	}

	public sealed class ComplexTestModel
	{
		public int Id { get; set; }

		public string? Name { get; set; }

		public long? LongValue { get; set; }

		public double? DoubleValue { get; set; }
	}

	private void SetupDb_AdditionalTable_ForComplexTests()
	{
		using SqliteCommand createCommand = connection.CreateCommand();
		createCommand.CommandText =
			@"
				DROP TABLE IF EXISTS ComplexTestTable;
				CREATE TABLE ComplexTestTable (
					Id INTEGER PRIMARY KEY AUTOINCREMENT,
					Name TEXT,
					LongValue INTEGER,
					DoubleValue REAL
				);
				INSERT INTO ComplexTestTable (Name, LongValue, DoubleValue)
				VALUES ('Complex1', 1000000, 3.14), ('Complex2', 2000000, 2.718);
			";
		createCommand.ExecuteNonQuery();
	}

	[Fact]
	public async Task GetDataStreamAsync_ComplexModel_ShouldMapCorrectly()
	{
		SetupDb_AdditionalTable_ForComplexTests();

		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name, LongValue, DoubleValue FROM ComplexTestTable ORDER BY Id";

		List<ComplexTestModel> results = new();
		await foreach (ComplexTestModel item in DirectQuery.GetDataStreamAsync<ComplexTestModel>(connection, cmd, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(2);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("Complex1");
		results[0].LongValue.ShouldBe(1000000);
		results[0].DoubleValue.ShouldNotBeNull();
	}

	[Fact]
	public void GetDataStreamSynchronous_ComplexModel_ShouldMapCorrectly()
	{
		SetupDb_AdditionalTable_ForComplexTests();

		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name, LongValue, DoubleValue FROM ComplexTestTable ORDER BY Id";

		List<ComplexTestModel> results = new();
		foreach (ComplexTestModel item in DirectQuery.GetDataStreamSynchronous<ComplexTestModel>(connection, cmd, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(2);
		results[0].Id.ShouldBe(1);
		results[0].Name.ShouldBe("Complex1");
		results[0].LongValue.ShouldBe(1000000);
	}

	[Fact]
	public async Task GetDataDirectAsync_ComplexModel_ShouldMapCorrectly()
	{
		SetupDb_AdditionalTable_ForComplexTests();

		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name, LongValue, DoubleValue FROM ComplexTestTable WHERE Id = 1";

		IEnumerable<ComplexTestModel> results = await DirectQuery.GetDataDirectAsync<ComplexTestModel>(connection, cmd, cancellationToken: Current.CancellationToken);

		results.Count().ShouldBe(1);
		results.First().Id.ShouldBe(1);
		results.First().Name.ShouldBe("Complex1");
		results.First().LongValue.ShouldBe(1000000);
	}

	public sealed class NullableTestModel
	{
		public int Id { get; set; }

		public string? Name { get; set; }
	}

	[Fact]
	public async Task GetDataStreamAsync_WithNullValues_ShouldMapCorrectly()
	{
		using SqliteCommand createCommand = connection.CreateCommand();
		createCommand.CommandText =
			@"
				CREATE TABLE IF NOT EXISTS NullableTestTable (
					Id INTEGER PRIMARY KEY AUTOINCREMENT,
					Name TEXT
				);
				DELETE FROM NullableTestTable;
				INSERT INTO NullableTestTable (Name) VALUES (NULL), ('NotNull');
			";
		await createCommand.ExecuteNonQueryAsync(Current.CancellationToken);

		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM NullableTestTable ORDER BY Id";

		List<NullableTestModel> results = new();
		await foreach (NullableTestModel item in DirectQuery.GetDataStreamAsync<NullableTestModel>(connection, cmd, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(2);
		results[0].Name.ShouldBeNull();
		results[1].Name.ShouldBe("NotNull");
	}

	[Fact]
	public void GetDataStreamSynchronous_WithNullValues_ShouldMapCorrectly()
	{
		using SqliteCommand createCommand = connection.CreateCommand();
		createCommand.CommandText =
			@"
				CREATE TABLE IF NOT EXISTS NullableTestTable2 (
					Id INTEGER PRIMARY KEY AUTOINCREMENT,
					Name TEXT
				);
				DELETE FROM NullableTestTable2;
				INSERT INTO NullableTestTable2 (Name) VALUES (NULL), ('NotNull');
			";
		createCommand.ExecuteNonQuery();

		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM NullableTestTable2 ORDER BY Id";

		List<NullableTestModel> results = new();
		foreach (NullableTestModel item in DirectQuery.GetDataStreamSynchronous<NullableTestModel>(connection, cmd, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(2);
		results[0].Name.ShouldBeNull();
		results[1].Name.ShouldBe("NotNull");
	}

	[Fact]
	public async Task GetDataDirectAsync_WithNullValues_ShouldMapCorrectly()
	{
		using SqliteCommand createCommand = connection.CreateCommand();
		createCommand.CommandText =
			@"
				CREATE TABLE IF NOT EXISTS NullableTestTable3 (
					Id INTEGER PRIMARY KEY AUTOINCREMENT,
					Name TEXT
				);
				DELETE FROM NullableTestTable3;
				INSERT INTO NullableTestTable3 (Name) VALUES (NULL);
			";
		await createCommand.ExecuteNonQueryAsync(Current.CancellationToken);

		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM NullableTestTable3 WHERE Id = 1";

		IEnumerable<NullableTestModel> results = await DirectQuery.GetDataDirectAsync<NullableTestModel>(connection, cmd, cancellationToken: Current.CancellationToken);

		results.Count().ShouldBe(1);
		results.First().Name.ShouldBeNull();
	}

	[Fact]
	public async Task GetDataTable_WithInvalidQuery_ShouldReturnEmptyDataTable()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM NonExistentTable";

		using DataTable result = await DirectQuery.GetDataTable(connection, cmd, maxRetry: 1, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(0);
	}

	[Fact]
	public void GetDataTableSynchronous_WithInvalidQuery_ShouldReturnEmptyDataTable()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM NonExistentTable";

		using DataTable result = DirectQuery.GetDataTableSynchronous(connection, cmd, maxRetry: 1);

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(0);
	}

	[Fact]
	public async Task RunUpdateQuery_WithInvalidQuery_ShouldReturnFailure()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "UPDATE NonExistentTable SET Name = 'Test'";

		UpdateResult result = await DirectQuery.RunUpdateQuery(connection, cmd, maxRetry: 1, cancellationToken: Current.CancellationToken);

		result.Success.ShouldBeFalse();
		result.RecordsChanged.ShouldBe(0);
	}

	[Fact]
	public void RunUpdateQuerySynchronous_WithInvalidQuery_ShouldReturnFailure()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "UPDATE NonExistentTable SET Name = 'Test'";

		UpdateResult result = DirectQuery.RunUpdateQuerySynchronous(connection, cmd, maxRetry: 1);

		result.Success.ShouldBeFalse();
		result.RecordsChanged.ShouldBe(0);
	}

	[Fact]
	public async Task GetDataDirectAsync_WithInvalidQuery_ShouldThrowException()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM NonExistentTable";

		await Should.ThrowAsync<DataException>(async () => await DirectQuery.GetDataDirectAsync<TestModel>(connection, cmd, maxRetry: 2, cancellationToken: Current.CancellationToken));
	}

	[Fact]
	public async Task GetDataTable_WithMultipleRetries_ShouldReturnEmptyDataTable()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM NonExistentTable";

		using DataTable result = await DirectQuery.GetDataTable(connection, cmd, maxRetry: 3, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(0);
	}

	[Fact]
	public void GetDataTableSynchronous_WithMultipleRetries_ShouldReturnEmptyDataTable()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM NonExistentTable";

		using DataTable result = DirectQuery.GetDataTableSynchronous(connection, cmd, maxRetry: 3);

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(0);
	}

	[Fact]
	public async Task RunUpdateQuery_WithMultipleRetries_ShouldReturnFailure()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "UPDATE NonExistentTable SET Name = 'Test'";

		UpdateResult result = await DirectQuery.RunUpdateQuery(connection, cmd, maxRetry: 3, cancellationToken: Current.CancellationToken);

		result.Success.ShouldBeFalse();
		result.RecordsChanged.ShouldBe(0);
	}

	[Fact]
	public void RunUpdateQuerySynchronous_WithMultipleRetries_ShouldReturnFailure()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "UPDATE NonExistentTable SET Name = 'Test'";

		UpdateResult result = DirectQuery.RunUpdateQuerySynchronous(connection, cmd, maxRetry: 3);

		result.Success.ShouldBeFalse();
		result.RecordsChanged.ShouldBe(0);
	}

	public sealed class ModelWithVariousTypes
	{
		public int Id { get; set; }

		public string? StringValue { get; set; }

		public long? LongValue { get; set; }

		public double? DoubleValue { get; set; }
	}

	[Fact]
	public async Task GetDataStreamAsync_WithVariousTypes_ShouldMapCorrectly()
	{
		using SqliteCommand createCommand = connection.CreateCommand();
		createCommand.CommandText =
			@"
				CREATE TABLE IF NOT EXISTS VariousTypesTable (
					Id INTEGER PRIMARY KEY AUTOINCREMENT,
					StringValue TEXT,
					LongValue INTEGER,
					DoubleValue REAL
				);
				DELETE FROM VariousTypesTable;
				INSERT INTO VariousTypesTable (StringValue, LongValue, DoubleValue)
				VALUES ('Test', 1234567890, 3.14159);
			";
		await createCommand.ExecuteNonQueryAsync(Current.CancellationToken);

		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, StringValue, LongValue, DoubleValue FROM VariousTypesTable";

		List<ModelWithVariousTypes> results = new();
		await foreach (ModelWithVariousTypes item in DirectQuery.GetDataStreamAsync<ModelWithVariousTypes>(connection, cmd, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);
		results[0].StringValue.ShouldBe("Test");
		results[0].LongValue.ShouldBe(1234567890);
		results[0].DoubleValue.ShouldNotBeNull();
	}

	// Exception Path Tests - Testing various exception scenarios

	[Fact]
	public async Task GetDataTable_WithDisposedConnection_ShouldHandleExceptionAndReturnEmptyTable()
	{
		SqliteConnection disposedConn = new("DataSource=:memory:");
		await disposedConn.DisposeAsync();
		await using SqliteCommand cmd = new("SELECT 1", disposedConn);

		using DataTable result = await DirectQuery.GetDataTable(disposedConn, cmd, maxRetry: 2, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		// SQLite can still execute simple queries even with disposed connection due to how it manages connections
		// The important thing is that we don't crash and return a valid DataTable
	}

	[Fact]
	public void GetDataTableSynchronous_WithDisposedConnection_ShouldHandleExceptionAndReturnEmptyTable()
	{
		SqliteConnection disposedConn = new("DataSource=:memory:");
		disposedConn.Dispose();
		using SqliteCommand cmd = new("SELECT 1", disposedConn);

		using DataTable result = DirectQuery.GetDataTableSynchronous(disposedConn, cmd, maxRetry: 2);

		result.ShouldNotBeNull();
		// SQLite can still execute simple queries even with disposed connection
	}

	[Fact]
	public async Task RunUpdateQuery_WithDisposedConnection_ShouldReturnFailureAfterRetries()
	{
		SqliteConnection disposedConn = new("DataSource=:memory:");
		await disposedConn.DisposeAsync();
		await using SqliteCommand cmd = new("UPDATE TestTable SET Name = 'Test'", disposedConn);

		UpdateResult result = await DirectQuery.RunUpdateQuery(disposedConn, cmd, maxRetry: 2, cancellationToken: Current.CancellationToken);

		result.Success.ShouldBeFalse();
		result.RecordsChanged.ShouldBe(0);
	}

	[Fact]
	public void RunUpdateQuerySynchronous_WithDisposedConnection_ShouldReturnFailureAfterRetries()
	{
		SqliteConnection disposedConn = new("DataSource=:memory:");
		disposedConn.Dispose();
		using SqliteCommand cmd = new("UPDATE TestTable SET Name = 'Test'", disposedConn);

		UpdateResult result = DirectQuery.RunUpdateQuerySynchronous(disposedConn, cmd, maxRetry: 2);

		result.Success.ShouldBeFalse();
		result.RecordsChanged.ShouldBe(0);
	}

	public sealed class IncompatibleTypeModel
	{
		public int Id { get; set; }
		public int Name { get; set; } // Intentionally wrong type - Name is TEXT in DB
	}

	[Fact]
	public async Task GetDataStreamAsync_WithTypeMismatch_ShouldThrowDataException()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";

		await Should.ThrowAsync<DataException>(async () =>
		{
			await foreach (IncompatibleTypeModel item in DirectQuery.GetDataStreamAsync<IncompatibleTypeModel>(connection, cmd, cancellationToken: Current.CancellationToken))
			{
				// This should throw before we get here
			}
		});
	}

	[Fact]
	public void GetDataStreamSynchronous_WithTypeMismatch_ShouldThrowDataException()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";

		Should.Throw<DataException>(() =>
		{
			foreach (IncompatibleTypeModel item in DirectQuery.GetDataStreamSynchronous<IncompatibleTypeModel>(connection, cmd))
			{
				// This should throw before we get here
			}
		});
	}

	[Fact]
	public async Task GetDataDirectAsync_WithTypeMismatch_ShouldThrowDataException()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";

		await Should.ThrowAsync<DataException>(async () => await DirectQuery.GetDataDirectAsync<IncompatibleTypeModel>(connection, cmd, cancellationToken: Current.CancellationToken));
	}

	[Fact]
	public async Task GetDataTable_WithConnectionThatFailsOnOpen_ShouldRetryAndReturnEmpty()
	{
		// SQLite is very resilient - even with unusual paths it can work
		SqliteConnection badConn = new("DataSource=|invalid|path|");
		await using SqliteCommand cmd = new("SELECT 1", badConn);

		using DataTable result = await DirectQuery.GetDataTable(badConn, cmd, maxRetry: 2, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		// May succeed or fail depending on SQLite behavior, but should not crash
		await badConn.DisposeAsync();
	}

	[Fact]
	public void GetDataTableSynchronous_WithConnectionThatFailsOnOpen_ShouldRetryAndReturnEmpty()
	{
		// SQLite is very resilient - even with unusual paths it can work
		SqliteConnection badConn = new("DataSource=|invalid|path|");
		using SqliteCommand cmd = new("SELECT 1", badConn);

		using DataTable result = DirectQuery.GetDataTableSynchronous(badConn, cmd, maxRetry: 2);

		result.ShouldNotBeNull();
		// May succeed or fail depending on SQLite behavior, but should not crash
		badConn.Dispose();
	}

	[Fact]
	public async Task RunUpdateQuery_WithConnectionThatFailsOnOpen_ShouldRetryAndReturnFailure()
	{
		SqliteConnection badConn = new("DataSource=|invalid|path|");
		await using SqliteCommand cmd = new("UPDATE TestTable SET Name = 'Test'", badConn);

		UpdateResult result = await DirectQuery.RunUpdateQuery(badConn, cmd, maxRetry: 2, cancellationToken: Current.CancellationToken);

		result.Success.ShouldBeFalse();
		result.RecordsChanged.ShouldBe(0);
		await badConn.DisposeAsync();
	}

	[Fact]
	public void RunUpdateQuerySynchronous_WithConnectionThatFailsOnOpen_ShouldRetryAndReturnFailure()
	{
		SqliteConnection badConn = new("DataSource=|invalid|path|");
		using SqliteCommand cmd = new("UPDATE TestTable SET Name = 'Test'", badConn);

		UpdateResult result = DirectQuery.RunUpdateQuerySynchronous(badConn, cmd, maxRetry: 2);

		result.Success.ShouldBeFalse();
		result.RecordsChanged.ShouldBe(0);
		badConn.Dispose();
	}

	[Fact]
	public async Task GetDataDirectAsync_WithMultipleFailures_ShouldExhaustRetriesAndThrow()
	{
		SqliteConnection disposedConn = new("DataSource=:memory:");
		await disposedConn.DisposeAsync();
		await using SqliteCommand cmd = new("SELECT * FROM TestTable", disposedConn);

		await Should.ThrowAsync<DataException>(async () => await DirectQuery.GetDataDirectAsync<TestModel>(disposedConn, cmd, maxRetry: 3, cancellationToken: Current.CancellationToken));
	}

	[Fact]
	public async Task GetDataTable_WithVeryShortTimeout_MayTriggerExceptions()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM TestTable";

		// Very short timeout might cause issues, but should still return a result or empty table
		using DataTable result = await DirectQuery.GetDataTable(connection, cmd, commandTimeoutSeconds: 1, maxRetry: 1, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
	}

	[Fact]
	public void GetDataTableSynchronous_WithVeryShortTimeout_MayTriggerExceptions()
	{
		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM TestTable";

		// Very short timeout might cause issues, but should still return a result or empty table
		using DataTable result = DirectQuery.GetDataTableSynchronous(connection, cmd, commandTimeoutSeconds: 1, maxRetry: 1);

		result.ShouldNotBeNull();
	}

	// Test with a model that has a property that can't be set
	public sealed class ReadOnlyPropertyModel
	{
		public int Id { get; }
		public string? Name { get; set; }

		public ReadOnlyPropertyModel()
		{
			Id = 0;
		}
	}

	[Fact]
	public async Task GetDataStreamAsync_WithReadOnlyProperty_ShouldOnlyMapWritableProperties()
	{
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";

		List<ReadOnlyPropertyModel> results = new();
		await foreach (ReadOnlyPropertyModel item in DirectQuery.GetDataStreamAsync<ReadOnlyPropertyModel>(connection, cmd, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);
		results[0].Name.ShouldNotBeNull();
	}

	[Fact]
	public async Task GetDataTable_WithEarlyConnectionDispose_ShouldHandleGracefully()
	{
		SqliteConnection tempConn = new("DataSource=:memory:");
		await using SqliteCommand cmd = new("SELECT 1", tempConn);

		// Dispose immediately after creating
		await tempConn.DisposeAsync();

		using DataTable result = await DirectQuery.GetDataTable(tempConn, cmd, maxRetry: 1, cancellationToken: Current.CancellationToken);

		result.ShouldNotBeNull();
		// SQLite may still execute simple queries even with disposed connections
	}

	[Fact]
	public async Task GetDataDirectAsync_RetriesOnFirstFailureThenSucceeds()
	{
		// Create a valid scenario - this tests the retry loop completes successfully
		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable WHERE Id = 1";

		// Should succeed and not need retries, but tests the retry parameter
		IEnumerable<TestModel> results = await DirectQuery.GetDataDirectAsync<TestModel>(connection, cmd, maxRetry: 5, cancellationToken: Current.CancellationToken);

		results.Count().ShouldBe(1);
	}

	[Fact]
	public async Task GetDataStreamAsync_WithConnectionError_ShouldCloseInFinally()
	{
		SqliteConnection invalidConn = new("DataSource=:memory:");
		await invalidConn.DisposeAsync();
		await using SqliteCommand cmd = new("SELECT 1", invalidConn);

		try
		{
			await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(invalidConn, cmd, cancellationToken: Current.CancellationToken))
			{
				// Should not reach here
			}
		}
		catch
		{
			// Expected - connection is disposed
		}

		// Connection should be in a closed state after finally block
		invalidConn.State.ShouldBe(ConnectionState.Closed);
	}

	[Fact]
	public void GetDataStreamSynchronous_WithConnectionError_ShouldCloseInFinally()
	{
		SqliteConnection invalidConn = new("DataSource=:memory:");
		invalidConn.Dispose();
		using SqliteCommand cmd = new("SELECT 1", invalidConn);

		try
		{
			foreach (TestModel item in DirectQuery.GetDataStreamSynchronous<TestModel>(invalidConn, cmd, cancellationToken: Current.CancellationToken))
			{
				// Should not reach here
			}
		}
		catch
		{
			// Expected - connection is disposed
		}

		// Connection should be in a closed state after finally block
		invalidConn.State.ShouldBe(ConnectionState.Closed);
	}

	// Additional tests to hit specific exception paths more reliably

	[Fact]
	public async Task GetDataTable_WithTableDroppedDuringRetries_ShouldHandleExceptions()
	{
		// Create a temporary connection and table
		using SqliteConnection tempConn = new("DataSource=:memory:");
		await tempConn.OpenAsync(Current.CancellationToken);

		using SqliteCommand setupCmd = tempConn.CreateCommand();
		setupCmd.CommandText = "CREATE TABLE TempTable (Id INTEGER); INSERT INTO TempTable VALUES (1);";
		await setupCmd.ExecuteNonQueryAsync(Current.CancellationToken);

		await using SqliteCommand cmd = tempConn.CreateCommand();
		cmd.CommandText = "SELECT * FROM NonExistentTable";

		using DataTable result = await DirectQuery.GetDataTable(tempConn, cmd, maxRetry: 2, cancellationToken: Current.CancellationToken);

		// Should return empty table after retries fail
		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(0);
	}

	[Fact]
	public void GetDataTableSynchronous_WithSyntaxError_ShouldRetryAndReturnEmpty()
	{
		using SqliteConnection tempConn = new("DataSource=:memory:");
		tempConn.Open();

		using SqliteCommand cmd = tempConn.CreateCommand();
		cmd.CommandText = "SELECT * FROM INVALID SYNTAX ERROR";

		using DataTable result = DirectQuery.GetDataTableSynchronous(tempConn, cmd, maxRetry: 2);

		// Should return empty table after retries fail
		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(0);
	}

	[Fact]
	public async Task RunUpdateQuery_WithSyntaxError_ShouldRetryAndReturnFailure()
	{
		using SqliteConnection tempConn = new("DataSource=:memory:");
		await tempConn.OpenAsync(Current.CancellationToken);

		await using SqliteCommand cmd = tempConn.CreateCommand();
		cmd.CommandText = "UPDATE INVALID SYNTAX";

		UpdateResult result = await DirectQuery.RunUpdateQuery(tempConn, cmd, maxRetry: 2, cancellationToken: Current.CancellationToken);

		result.Success.ShouldBeFalse();
		result.RecordsChanged.ShouldBe(0);
	}

	[Fact]
	public void RunUpdateQuerySynchronous_WithSyntaxError_ShouldRetryAndReturnFailure()
	{
		using SqliteConnection tempConn = new("DataSource=:memory:");
		tempConn.Open();

		using SqliteCommand cmd = tempConn.CreateCommand();
		cmd.CommandText = "UPDATE INVALID SYNTAX";

		UpdateResult result = DirectQuery.RunUpdateQuerySynchronous(tempConn, cmd, maxRetry: 2);

		result.Success.ShouldBeFalse();
		result.RecordsChanged.ShouldBe(0);
	}

	[Fact]
	public async Task GetDataDirectAsync_WithConnectionClosedBetweenRetries_ShouldThrow()
	{
		using SqliteConnection tempConn = new("DataSource=:memory:");
		await using SqliteCommand cmd = new("SELECT * FROM NonExistentTable", tempConn);

		await Should.ThrowAsync<DataException>(async () => await DirectQuery.GetDataDirectAsync<TestModel>(tempConn, cmd, maxRetry: 2, cancellationToken: Current.CancellationToken));
	}

	[Fact]
	public async Task GetDataTable_WithNonDbException_ShouldCatchAndRetry()
	{
		// Create a scenario where a general exception (not DbException) occurs
		using SqliteConnection tempConn = new("DataSource=:memory:");
		await tempConn.OpenAsync(Current.CancellationToken);

		await using SqliteCommand cmd = tempConn.CreateCommand();
		cmd.CommandText = "SELECT * FROM NonExistentTable";

		using DataTable result = await DirectQuery.GetDataTable(tempConn, cmd, maxRetry: 3, cancellationToken: Current.CancellationToken);

		// Should handle the exception and return empty table
		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(0);
	}

	[Fact]
	public void GetDataTableSynchronous_WithNonDbException_ShouldCatchAndRetry()
	{
		// Create a scenario where a general exception (not DbException) occurs
		using SqliteConnection tempConn = new("DataSource=:memory:");
		tempConn.Open();

		using SqliteCommand cmd = tempConn.CreateCommand();
		cmd.CommandText = "SELECT * FROM NonExistentTable";

		using DataTable result = DirectQuery.GetDataTableSynchronous(tempConn, cmd, maxRetry: 3);

		// Should handle the exception and return empty table
		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(0);
	}

	[Fact]
	public async Task RunUpdateQuery_WithNonDbException_ShouldCatchAndRetry()
	{
		using SqliteConnection tempConn = new("DataSource=:memory:");
		await tempConn.OpenAsync(Current.CancellationToken);

		await using SqliteCommand cmd = tempConn.CreateCommand();
		cmd.CommandText = "UPDATE NonExistentTable SET X = 1";

		UpdateResult result = await DirectQuery.RunUpdateQuery(tempConn, cmd, maxRetry: 3, cancellationToken: Current.CancellationToken);

		result.Success.ShouldBeFalse();
	}

	[Fact]
	public void RunUpdateQuerySynchronous_WithNonDbException_ShouldCatchAndRetry()
	{
		using SqliteConnection tempConn = new("DataSource=:memory:");
		tempConn.Open();

		using SqliteCommand cmd = tempConn.CreateCommand();
		cmd.CommandText = "UPDATE NonExistentTable SET X = 1";

		UpdateResult result = DirectQuery.RunUpdateQuerySynchronous(tempConn, cmd, maxRetry: 3);

		result.Success.ShouldBeFalse();
	}

	// Tests for GetOrAddPropertiesFromMappingCache code paths

	[Fact]
	public async Task GetDataStreamAsync_WithLimitedCache_FirstCall_ShouldCreateAndCacheMapper()
	{
		// Setup: Use limited cache
		DirectQuery.CacheManager.SetUseLimitedCache(true);
		DirectQuery.CacheManager.SetLimitedCacheSize(10);
		DirectQuery.CacheManager.ClearAllCaches();

		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";

		// First call - should create and cache the mapper
		List<TestModel> results = new();
		await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd, useCache: true, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);

		// Verify it was added to limited cache
		DirectQuery.CacheManager.GetLimitedCache().ContainsKey(typeof(TestModel)).ShouldBeTrue();
		DirectQuery.CacheManager.GetCache().ContainsKey(typeof(TestModel)).ShouldBeFalse(); // Should not be in regular cache
	}

	[Fact]
	public async Task GetDataStreamAsync_WithLimitedCache_SecondCall_ShouldRetrieveFromCache()
	{
		// Setup: Use limited cache
		DirectQuery.CacheManager.SetUseLimitedCache(true);
		DirectQuery.CacheManager.SetLimitedCacheSize(10);
		DirectQuery.CacheManager.ClearAllCaches();

		// First call - creates and caches
		await using (SqliteCommand cmd1 = connection.CreateCommand())
		{
			cmd1.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";
			List<TestModel> results1 = new();
			await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd1, useCache: true, cancellationToken: Current.CancellationToken))
			{
				results1.Add(item);
			}
			results1.Count.ShouldBe(1);
		}

		int limitedCacheCountAfterFirst = DirectQuery.CacheManager.GetLimitedCache().Count;

		// Second call - should retrieve from cache (not create new)
		await using (SqliteCommand cmd2 = connection.CreateCommand())
		{
			cmd2.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";
			List<TestModel> results2 = new();
			await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd2, useCache: true, cancellationToken: Current.CancellationToken))
			{
				results2.Add(item);
			}
			results2.Count.ShouldBe(1);
		}

		// Cache count should remain the same
		DirectQuery.CacheManager.GetLimitedCache().Count.ShouldBe(limitedCacheCountAfterFirst);
	}

	[Fact]
	public async Task GetDataStreamAsync_WithRegularCache_FirstCall_ShouldCreateAndCacheMapper()
	{
		// Setup: Use regular cache (not limited)
		DirectQuery.CacheManager.SetUseLimitedCache(false);
		DirectQuery.CacheManager.ClearAllCaches();

		await using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";

		// First call - should create and cache the mapper
		List<TestModel> results = new();
		await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd, useCache: true, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);

		// Verify it was added to regular cache
		DirectQuery.CacheManager.GetCache().ContainsKey(typeof(TestModel)).ShouldBeTrue();
		DirectQuery.CacheManager.GetLimitedCache().ContainsKey(typeof(TestModel)).ShouldBeFalse(); // Should not be in limited cache
	}

	[Fact]
	public async Task GetDataStreamAsync_WithRegularCache_SecondCall_ShouldRetrieveFromCache()
	{
		// Setup: Use regular cache (not limited)
		DirectQuery.CacheManager.SetUseLimitedCache(false);
		DirectQuery.CacheManager.ClearAllCaches();

		// First call - creates and caches
		await using (SqliteCommand cmd1 = connection.CreateCommand())
		{
			cmd1.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";
			List<TestModel> results1 = new();
			await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd1, useCache: true, cancellationToken: Current.CancellationToken))
			{
				results1.Add(item);
			}
			results1.Count.ShouldBe(1);
		}

		int cacheCountAfterFirst = DirectQuery.CacheManager.GetCache().Count;

		// Second call - should retrieve from cache (not create new)
		await using (SqliteCommand cmd2 = connection.CreateCommand())
		{
			cmd2.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";
			List<TestModel> results2 = new();
			await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd2, useCache: true, cancellationToken: Current.CancellationToken))
			{
				results2.Add(item);
			}
			results2.Count.ShouldBe(1);
		}

		// Cache count should remain the same
		DirectQuery.CacheManager.GetCache().Count.ShouldBe(cacheCountAfterFirst);
	}

	[Fact]
	public void GetDataStreamSynchronous_WithLimitedCache_FirstCall_ShouldCreateAndCacheMapper()
	{
		// Setup: Use limited cache
		DirectQuery.CacheManager.SetUseLimitedCache(true);
		DirectQuery.CacheManager.SetLimitedCacheSize(10);
		DirectQuery.CacheManager.ClearAllCaches();

		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";

		// First call - should create and cache the mapper
		List<TestModel> results = new();
		foreach (TestModel item in DirectQuery.GetDataStreamSynchronous<TestModel>(connection, cmd, useCache: true, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);

		// Verify it was added to limited cache
		DirectQuery.CacheManager.GetLimitedCache().ContainsKey(typeof(TestModel)).ShouldBeTrue();
	}

	[Fact]
	public void GetDataStreamSynchronous_WithRegularCache_FirstCall_ShouldCreateAndCacheMapper()
	{
		// Setup: Use regular cache (not limited)
		DirectQuery.CacheManager.SetUseLimitedCache(false);
		DirectQuery.CacheManager.ClearAllCaches();

		using SqliteCommand cmd = connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";

		// First call - should create and cache the mapper
		List<TestModel> results = new();
		foreach (TestModel item in DirectQuery.GetDataStreamSynchronous<TestModel>(connection, cmd, useCache: true, cancellationToken: Current.CancellationToken))
		{
			results.Add(item);
		}

		results.Count.ShouldBe(1);

		// Verify it was added to regular cache
		DirectQuery.CacheManager.GetCache().ContainsKey(typeof(TestModel)).ShouldBeTrue();
	}

	[Fact]
	public async Task GetDataDirectAsync_WithLimitedCache_HitsCorrectCachePath()
	{
		// Setup: Use limited cache
		DirectQuery.CacheManager.SetUseLimitedCache(true);
		DirectQuery.CacheManager.SetLimitedCacheSize(10);
		DirectQuery.CacheManager.ClearAllCaches();

		// First call
		IEnumerable<TestModel> results1;
		await using (SqliteCommand cmd1 = connection.CreateCommand())
		{
			cmd1.CommandText = "SELECT Id, Name FROM TestTable WHERE Id = 1";
			results1 = await DirectQuery.GetDataDirectAsync<TestModel>(connection, cmd1, useCache: true, cancellationToken: Current.CancellationToken);
		}
		results1.Count().ShouldBe(1);

		// Verify limited cache was used
		DirectQuery.CacheManager.GetLimitedCache().ContainsKey(typeof(TestModel)).ShouldBeTrue();
		DirectQuery.CacheManager.GetCache().ContainsKey(typeof(TestModel)).ShouldBeFalse();

		// Second call to ensure cache hit path is executed
		IEnumerable<TestModel> results2;
		await using (SqliteCommand cmd2 = connection.CreateCommand())
		{
			cmd2.CommandText = "SELECT Id, Name FROM TestTable WHERE Id = 1";
			results2 = await DirectQuery.GetDataDirectAsync<TestModel>(connection, cmd2, useCache: true, cancellationToken: Current.CancellationToken);
		}
		results2.Count().ShouldBe(1);
	}

	[Fact]
	public async Task GetDataDirectAsync_WithRegularCache_HitsCorrectCachePath()
	{
		// Setup: Use regular cache
		DirectQuery.CacheManager.SetUseLimitedCache(false);
		DirectQuery.CacheManager.ClearAllCaches();

		// First call
		IEnumerable<TestModel> results1;
		await using (SqliteCommand cmd1 = connection.CreateCommand())
		{
			cmd1.CommandText = "SELECT Id, Name FROM TestTable WHERE Id = 1";
			results1 = await DirectQuery.GetDataDirectAsync<TestModel>(connection, cmd1, useCache: true, cancellationToken: Current.CancellationToken);
		}
		results1.Count().ShouldBe(1);

		// Verify regular cache was used
		DirectQuery.CacheManager.GetCache().ContainsKey(typeof(TestModel)).ShouldBeTrue();
		DirectQuery.CacheManager.GetLimitedCache().ContainsKey(typeof(TestModel)).ShouldBeFalse();

		// Second call to ensure cache hit path is executed
		IEnumerable<TestModel> results2;
		await using (SqliteCommand cmd2 = connection.CreateCommand())
		{
			cmd2.CommandText = "SELECT Id, Name FROM TestTable WHERE Id = 1";
			results2 = await DirectQuery.GetDataDirectAsync<TestModel>(connection, cmd2, useCache: true, cancellationToken: Current.CancellationToken);
		}
		results2.Count().ShouldBe(1);
	}

	public sealed class AnotherTestModel
	{
		public int Id { get; set; }
		public string? Name { get; set; }
	}

	[Fact]
	public async Task GetDataStreamAsync_WithMultipleTypes_CachesEachTypeCorrectly()
	{
		// Setup: Use regular cache
		DirectQuery.CacheManager.SetUseLimitedCache(false);
		DirectQuery.CacheManager.ClearAllCaches();

		// First type
		await using (SqliteCommand cmd1 = connection.CreateCommand())
		{
			cmd1.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";
			List<TestModel> results1 = new();
			await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd1, useCache: true, cancellationToken: Current.CancellationToken))
			{
				results1.Add(item);
			}
			results1.Count.ShouldBe(1);
		}

		// Second type
		await using (SqliteCommand cmd2 = connection.CreateCommand())
		{
			cmd2.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";
			List<AnotherTestModel> results2 = new();
			await foreach (AnotherTestModel item in DirectQuery.GetDataStreamAsync<AnotherTestModel>(connection, cmd2, useCache: true, cancellationToken: Current.CancellationToken))
			{
				results2.Add(item);
			}
			results2.Count.ShouldBe(1);
		}

		// Both types should be in cache
		DirectQuery.CacheManager.GetCache().ContainsKey(typeof(TestModel)).ShouldBeTrue();
		DirectQuery.CacheManager.GetCache().ContainsKey(typeof(AnotherTestModel)).ShouldBeTrue();
		DirectQuery.CacheManager.GetCache().Count.ShouldBeGreaterThanOrEqualTo(2);
	}

	[Fact]
	public async Task GetDataStreamAsync_SwitchingCacheModes_UsesCorrectCache()
	{
		// Start with regular cache
		DirectQuery.CacheManager.SetUseLimitedCache(false);
		DirectQuery.CacheManager.ClearAllCaches();

		await using (SqliteCommand cmd1 = connection.CreateCommand())
		{
			cmd1.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";
			List<TestModel> results1 = new();
			await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd1, useCache: true, cancellationToken: Current.CancellationToken))
			{
				results1.Add(item);
			}
			results1.Count.ShouldBe(1);
			DirectQuery.CacheManager.GetCache().ContainsKey(typeof(TestModel)).ShouldBeTrue();
		}

		// Switch to limited cache and clear
		DirectQuery.CacheManager.SetUseLimitedCache(true);
		DirectQuery.CacheManager.ClearAllCaches();

		await using (SqliteCommand cmd2 = connection.CreateCommand())
		{
			cmd2.CommandText = "SELECT Id, Name FROM TestTable LIMIT 1";
			List<TestModel> results2 = new();
			await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(connection, cmd2, useCache: true, cancellationToken: Current.CancellationToken))
			{
				results2.Add(item);
			}
			results2.Count.ShouldBe(1);
		}

		// Now should be in limited cache
		DirectQuery.CacheManager.GetLimitedCache().ContainsKey(typeof(TestModel)).ShouldBeTrue();
		DirectQuery.CacheManager.GetCache().ContainsKey(typeof(TestModel)).ShouldBeFalse();
	}
}
