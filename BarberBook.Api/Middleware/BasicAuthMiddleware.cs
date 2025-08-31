using System.Net.Http.Headers;
using System.Text;

namespace BarberBook.Api.Middleware;

public sealed class BasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BasicAuthMiddleware> _logger;
    private readonly string _username;
    private readonly string _password;

    public BasicAuthMiddleware(RequestDelegate next, IConfiguration config, ILogger<BasicAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _username = config["Admin:Username"] ?? "admin";
        _password = config["Admin:Password"] ?? "admin";
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            Challenge(context);
            return;
        }

        if (!AuthenticationHeaderValue.TryParse(authHeader, out var auth) || auth.Scheme != "Basic")
        {
            Challenge(context);
            return;
        }

        var creds = Encoding.UTF8.GetString(Convert.FromBase64String(auth.Parameter ?? ""));
        var parts = creds.Split(':', 2);
        if (parts.Length != 2 || parts[0] != _username || parts[1] != _password)
        {
            Challenge(context);
            return;
        }

        await _next(context);
    }

    private static void Challenge(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Admin\"";
    }
}

