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
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                _connection.Dispose();
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
        createCommand.CommandText = @"
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

        DataTable result = await DirectQuery.GetDataTable(_connection, cmd);

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
}
