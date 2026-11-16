using System.Net;

namespace Library.Api.Tests;

public class AuthTests : IClassFixture<LibraryFactory>
{
    private readonly LibraryFactory _f;
    public AuthTests(LibraryFactory f) => _f = f;

    [Fact]
    public async Task Healthz_is_public()
    {
        var res = await _f.CreateClient().GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/books")]
    [InlineData("GET", "/api/me")]
    [InlineData("POST", "/api/books")]
    [InlineData("DELETE", "/api/books/1")]
    [InlineData("DELETE", "/api/reviews/1")]
    public async Task Api_requires_login(string method, string url)
    {
        var res = await _f.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), url));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_is_rejected_and_sets_no_cookie()
    {
        var res = await _f.CreateClient().PostAsJsonAsync("/api/login", new { password = "nope" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.False(res.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Missing_password_is_rejected()
    {
        var res = await _f.CreateClient().PostAsJsonAsync("/api/login", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_sets_httponly_cookie_that_grants_access_until_logout()
    {
        var client = _f.CreateClient();
        var login = await client.PostAsJsonAsync("/api/login", new { password = LibraryFactory.Password });
        Assert.Equal(HttpStatusCode.NoContent, login.StatusCode);
        Assert.Contains(login.Headers.GetValues("Set-Cookie"), c => c.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(HttpStatusCode.NoContent, (await client.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/books")).StatusCode);

        await client.PostAsync("/api/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Login_is_rate_limited()
    {
        using var limited = new LibraryFactory(loginPermits: 3);
        var client = limited.CreateClient();
        var codes = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
            codes.Add((await client.PostAsJsonAsync("/api/login", new { password = "x" })).StatusCode);
        Assert.Equal(
            [HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized,
             HttpStatusCode.TooManyRequests, HttpStatusCode.TooManyRequests], codes);
    }
}
