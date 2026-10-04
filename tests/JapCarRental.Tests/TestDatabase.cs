using JapCarRental.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JapCarRental.Tests;

/// <summary>
/// In-memory SQLite database for tests. The connection must stay open,
/// otherwise the in-memory database disappears.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public AppDbContext Context { get; }

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public AppDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection);

        if (interceptors.Length > 0)
        {
            builder.AddInterceptors(interceptors);
        }

        return new AppDbContext(builder.Options);
    }

    public AppDbContext CreateSecondContext() => CreateContext();

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}