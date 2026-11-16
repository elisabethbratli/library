using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace Library.Api.Auth;

public record LoginInput(string? Password);

/// <summary>Shared-password auth: POST /api/login issues an HttpOnly session cookie.</summary>
public static class AuthSetup
{
    public const string LoginLimiter = "login";

    public static void AddPasswordAuth(this WebApplicationBuilder builder)
    {
        if (string.IsNullOrEmpty(builder.Configuration["Auth:Password"]))
            throw new InvalidOperationException("Auth__Password must be set");

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.Cookie.Name = "library_session";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                o.ExpireTimeSpan = TimeSpan.FromDays(30);
                o.SlidingExpiration = true;
                // An API answers 401/403 instead of redirecting to a login page.
                o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
                o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
            });

        builder.Services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        // Global limit: behind the proxy every client appears with the same address.
        var permits = builder.Configuration.GetValue("Auth:LoginPermitLimit", 10);
        builder.Services.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            o.AddFixedWindowLimiter(LoginLimiter, l =>
            {
                l.PermitLimit = permits;
                l.Window = TimeSpan.FromMinutes(1);
                l.QueueLimit = 0;
            });
        });
    }

    public static void MapAuthEndpoints(this RouteGroupBuilder api, string password)
    {
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes(password));

        api.MapPost("/login", async (LoginInput input, HttpContext ctx) =>
        {
            var given = SHA256.HashData(Encoding.UTF8.GetBytes(input.Password ?? ""));
            if (!CryptographicOperations.FixedTimeEquals(given, expected))
                return Results.Json(new { error = "Wrong password" }, statusCode: StatusCodes.Status401Unauthorized);
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "owner")], CookieAuthenticationDefaults.AuthenticationScheme));
            await ctx.SignInAsync(principal, new AuthenticationProperties { IsPersistent = true });
            return Results.NoContent();
        }).AllowAnonymous().RequireRateLimiting(LoginLimiter);

        api.MapPost("/logout", async (HttpContext ctx) =>
        {
            await ctx.SignOutAsync();
            return Results.NoContent();
        }).AllowAnonymous();

        api.MapGet("/me", () => Results.NoContent()); // 204 when logged in, 401 otherwise
    }
}
