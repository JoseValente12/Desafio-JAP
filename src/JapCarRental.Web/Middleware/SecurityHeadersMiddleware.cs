namespace JapCarRental.Web.Middleware;

/// <summary>Adds the security headers that tell the browser what a page is allowed to do.</summary>
public class SecurityHeadersMiddleware
{
    // Everything comes from our own origin, except the Google Fonts files.
    // No 'unsafe-inline' and no 'unsafe-eval': scripts and styles live in files.
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self' https://fonts.googleapis.com; " +
        "font-src https://fonts.gstatic.com; " +
        "img-src 'self' data:; " +
        "connect-src 'self'; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["Content-Security-Policy"] = ContentSecurityPolicy;
        headers["X-Content-Type-Options"] = "nosniff";      // do not guess a file's type
        headers["X-Frame-Options"] = "DENY";                // no framing (clickjacking); old browsers
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        return _next(context);
    }
}