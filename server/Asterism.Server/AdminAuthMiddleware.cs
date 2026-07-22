namespace Asterism.Server;

public sealed class AdminAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _adminApiKey;

    public AdminAuthMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _adminApiKey = config["Asterism:AdminApiKey"]
            ?? throw new InvalidOperationException("Asterism:AdminApiKey が設定されていません。");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api/admin"))
        {
            await _next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        var provided = header.StartsWith("Bearer ") ? header["Bearer ".Length..] : null;

        if (string.IsNullOrEmpty(provided) || provided != _adminApiKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }
}
