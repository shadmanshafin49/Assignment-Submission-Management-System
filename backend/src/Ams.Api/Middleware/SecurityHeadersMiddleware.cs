namespace Ams.Api.Middleware;

/// <summary>
/// Adds the response headers that are cheap, have no downside for a JSON API, and close the one
/// gap this app actually has: attachment downloads.
/// <para>
/// An attachment's <c>Content-Type</c> is whatever the uploader's browser claimed at upload time,
/// and it is echoed back verbatim on download. <c>File(...)</c> already sends
/// <c>Content-Disposition: attachment</c>, so the bytes are saved rather than rendered — but
/// <c>nosniff</c> is what stops a browser second-guessing that from the bytes themselves.
/// </para>
/// <para>
/// Registered through <c>OnStarting</c> rather than by writing the headers directly, because
/// <see cref="ExceptionHandlingMiddleware"/> calls <c>Response.Clear()</c> when it turns an
/// exception into ProblemDetails. Headers written on the way in would be discarded by that;
/// an <c>OnStarting</c> callback runs at flush time, after any clear, so error responses carry
/// the headers too.
/// </para>
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var headers = ((HttpContext)state).Response.Headers;

            // Never sniff a declared content type — see the class summary.
            headers["X-Content-Type-Options"] = "nosniff";

            // Nothing here is meant to be framed, including the Swagger UI.
            headers["X-Frame-Options"] = "DENY";

            // Resource paths carry record ids; keep them out of the Referer header
            // on any link that leads off the origin.
            headers["Referrer-Policy"] = "no-referrer";

            return Task.CompletedTask;
        }, context);

        return next(context);
    }
}
