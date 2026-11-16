using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;

namespace Library.Api.Tests;

/// <summary>
/// Boots the API against a throwaway database on a real PostgreSQL server.
/// Set LIBRARY_TEST_DB to a connection string for a server where the user may create databases,
/// e.g. "Host=localhost;Port=5432;Username=postgres;Password=..." (do not include Database).
/// </summary>
public class LibraryFactory : WebApplicationFactory<Program>
{
    public const string Password = "test-password";
    private readonly string _admin = Environment.GetEnvironmentVariable("LIBRARY_TEST_DB")
        ?? throw new InvalidOperationException("Set LIBRARY_TEST_DB (see LibraryFactory docs)");
    private readonly string _dbName = $"library_test_{Guid.NewGuid():N}";
    private readonly int _loginPermits;

    public LibraryFactory() : this(1000) { }
    internal LibraryFactory(int loginPermits) => _loginPermits = loginPermits;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Library", $"{_admin};Database={_dbName}");
        builder.UseSetting("Auth:Password", Password);
        builder.UseSetting("Auth:LoginPermitLimit", _loginPermits.ToString());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;
        NpgsqlConnection.ClearAllPools();
        using var c = new NpgsqlConnection($"{_admin};Database=postgres");
        c.Open();
        using var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_dbName}\" WITH (FORCE)", c);
        cmd.ExecuteNonQuery();
    }

    /// <summary>A client that has already logged in.</summary>
    public async Task<HttpClient> LoggedInClientAsync()
    {
        var client = CreateClient();
        var res = await client.PostAsJsonAsync("/api/login", new { password = Password });
        res.EnsureSuccessStatusCode();
        return client;
    }
}
