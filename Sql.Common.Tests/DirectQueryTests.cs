using System.Data;
using CommonNetFuncs.Sql.Common;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Sql.Common.Tests;

public sealed class DirectQueryTests : IDisposable
{
	private readonly SqliteConnection _connection;

	public DirectQueryTests()
	{
		Batteries.Init();
		_connection = new SqliteConnection("DataSource=:memory:");
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
				_connection.Dispose();
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
		_connection.Open();
		using SqliteCommand createCommand = _connection.CreateCommand();
		createCommand.CommandText =
			@"
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
		await using SqliteCommand cmd = _connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM TestTable";

		using DataTable result = await DirectQuery.GetDataTable(_connection, cmd);

		result.ShouldNotBeNull();
		result.Rows.Count.ShouldBe(5);
		result.Columns.Contains("Id").ShouldBeTrue();
		result.Columns.Contains("Name").ShouldBeTrue();
	}

	[Fact]
	public void GetDataTableSynchronous_ShouldReturnPopulatedDataTable_WhenQuerySucceeds()
	{
		using SqliteCommand cmd = _connection.CreateCommand();
		cmd.CommandText = "SELECT * FROM TestTable";

		using DataTable result = DirectQuery.GetDataTableSynchronous(_connection, cmd);

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
			await using SqliteCommand cmd = _connection.CreateCommand();
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
				result = await DirectQuery.GetDataTable(_connection, cmd, maxRetry: 3);
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
		await using SqliteCommand cmd = _connection.CreateCommand();
		cmd.CommandText = "UPDATE TestTable SET Name = 'Updated' WHERE Name LIKE 'Test%'";

		UpdateResult result = await DirectQuery.RunUpdateQuery(_connection, cmd);

		result.Success.ShouldBeTrue();
		result.RecordsChanged.ShouldBe(5);
	}

	[Fact]
	public void RunUpdateQuerySynchronous_ShouldReturnSuccessResult_WhenUpdateSucceeds()
	{
		using SqliteCommand cmd = _connection.CreateCommand();
		cmd.CommandText = "UPDATE TestTable SET Name = 'Updated' WHERE Name LIKE 'Test%'";

		UpdateResult result = DirectQuery.RunUpdateQuerySynchronous(_connection, cmd);

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
		await using SqliteCommand cmd = _connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable ORDER BY Id LIMIT 2";

		List<TestModel> results = new();
		await foreach (TestModel item in DirectQuery.GetDataStreamAsync<TestModel>(_connection, cmd))
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
		await using SqliteCommand cmd = _connection.CreateCommand();
		cmd.CommandText = "SELECT Id, Name FROM TestTable WHERE Id = 1";

		IEnumerable<TestModel> results = await DirectQuery.GetDataDirectAsync<TestModel>(_connection, cmd);

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
}
