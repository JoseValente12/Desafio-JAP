using JapCarRental.Web.Middleware;
using Microsoft.AspNetCore.Http;

namespace JapCarRental.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private static async Task<IHeaderDictionary> RunAsync()
    {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        return context.Response.Headers;
    }

    [Fact]
    public async Task AddsTheBasicSecurityHeaders()
    {
        var headers = await RunAsync();

        Assert.Equal("nosniff", headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", headers["X-Frame-Options"]);
        Assert.False(string.IsNullOrEmpty(headers["Referrer-Policy"]));
    }

    // Guards against someone loosening the policy "just to make it work".
    [Fact]
    public async Task ContentSecurityPolicy_ForbidsInlineAndEvalScripts()
    {
        var policy = (await RunAsync())["Content-Security-Policy"].ToString();

        Assert.Contains("frame-ancestors 'none'", policy);
        Assert.Contains("script-src 'self'", policy);
        Assert.DoesNotContain("unsafe-inline", policy);
        Assert.DoesNotContain("unsafe-eval", policy);
    }
}
