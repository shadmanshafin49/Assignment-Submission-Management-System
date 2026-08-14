using System.Net;
using System.Text.Json;
using Ams.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace Ams.UnitTests.Api;

/// <summary>
/// The two middlewares that shape every response, driven through a real host pipeline rather
/// than a <c>DefaultHttpContext</c>. That distinction matters here:
/// <see cref="SecurityHeadersMiddleware"/> writes its headers from an <c>OnStarting</c>
/// callback, and <c>DefaultHttpContext</c>'s response feature never fires those — a test
/// against it would pass while asserting nothing.
/// </summary>
public class ResponsePipelineTests
{
    /// <summary>Boots the two middlewares over a handler that either succeeds or throws.</summary>
    private static async Task<WebApplication> StartAsync(string environment, Exception? throws = null)
    {
        var builder = WebApplication.CreateSlimBuilder(
            new WebApplicationOptions { EnvironmentName = environment });

        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        var app = builder.Build();

        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.Run(context => throws is not null
            ? throw throws
            : context.Response.WriteAsync("""{"ok":true}"""));

        await app.StartAsync();
        return app;
    }

    // ------------------------------------------------------------- error detail

    [Fact]
    public async Task An_unexpected_failure_leaks_no_internals_in_production()
    {
        // The compose stack used to run the API with ASPNETCORE_ENVIRONMENT=Development, which
        // put the full .NET stack trace — types, file paths, line numbers — in the response body
        // of every 500. This is the assertion that stops that coming back.
        await using var app = await StartAsync(
            Environments.Production,
            new InvalidOperationException("connection string Host=db;Password=hunter2 failed"));

        var response = await app.GetTestClient().GetAsync("/boom");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        body.ShouldNotContain("hunter2");
        body.ShouldNotContain("InvalidOperationException");
        body.ShouldNotContain("at Ams.");

        using var problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("detail").GetString().ShouldBe("Please try again later.");
        problem.RootElement.GetProperty("errorCode").GetString().ShouldBe("internal_error");
    }

    [Fact]
    public async Task An_unexpected_failure_still_shows_its_detail_in_development()
    {
        // The other half of the rule: a developer running locally keeps the stack trace,
        // which is the only reason the branch exists.
        await using var app = await StartAsync(
            Environments.Development, new InvalidOperationException("boom"));

        var response = await app.GetTestClient().GetAsync("/boom");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        body.ShouldContain("InvalidOperationException");
    }

    // --------------------------------------------------------- security headers

    [Fact]
    public async Task Every_response_carries_the_security_headers()
    {
        await using var app = await StartAsync(Environments.Production);

        var response = await app.GetTestClient().GetAsync("/fine");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Header(response, "X-Content-Type-Options").ShouldBe("nosniff");
        Header(response, "X-Frame-Options").ShouldBe("DENY");
        Header(response, "Referrer-Policy").ShouldBe("no-referrer");
    }

    [Fact]
    public async Task The_security_headers_survive_the_error_handler_clearing_the_response()
    {
        // ExceptionHandlingMiddleware calls Response.Clear() before writing ProblemDetails.
        // Headers written on the way in would be discarded by that; these are registered
        // through OnStarting so they land after the clear. An attachment download that
        // 500s must still be nosniff.
        await using var app = await StartAsync(
            Environments.Production, new InvalidOperationException("boom"));

        var response = await app.GetTestClient().GetAsync("/boom");

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        Header(response, "X-Content-Type-Options").ShouldBe("nosniff");
        Header(response, "X-Frame-Options").ShouldBe("DENY");
    }

    private static string? Header(HttpResponseMessage response, string name)
        => response.Headers.TryGetValues(name, out var values)
            ? string.Join(",", values)
            : null;
}
