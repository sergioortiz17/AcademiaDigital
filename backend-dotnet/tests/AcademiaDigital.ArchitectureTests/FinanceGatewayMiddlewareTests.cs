using System.Net;
using System.Text;
using AcademiaDigital.API.Middleware;
using AcademiaDigital.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AcademiaDigital.ArchitectureTests;

public sealed class FinanceGatewayMiddlewareTests
{
    [Fact]
    public async Task Gateway_replaces_untrusted_identity_headers_with_active_session_identity()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json")
        });
        var context = FinanceContext();
        context.Items["UserId"] = 42L;
        context.Items["UserRole"] = (int)UserRole.Admin;
        context.Request.Headers["X-User-Id"] = "999";
        context.Request.Headers["X-User-Role"] = "Alumno";

        await InvokeAsync(context, handler);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("42", handler.UserId);
        Assert.Equal("Admin", handler.UserRole);
    }

    [Fact]
    public async Task Gateway_rejects_a_request_without_an_active_session()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var context = FinanceContext();

        await InvokeAsync(context, handler);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task Gateway_returns_recoverable_503_when_finance_is_unavailable()
    {
        var handler = new CapturingHandler(_ => throw new HttpRequestException("offline"));
        var context = FinanceContext();
        context.Items["UserId"] = 7L;
        context.Items["UserRole"] = (int)UserRole.Alumno;

        await InvokeAsync(context, handler);

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        Assert.Contains("puede continuar", body);
    }

    private static DefaultHttpContext FinanceContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/finance/concepts";
        context.Request.Headers["Accept"] = "application/json";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static Task InvokeAsync(HttpContext context, HttpMessageHandler handler)
    {
        var middleware = new FinanceGatewayMiddleware(_ => Task.CompletedTask);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Finance:BaseUrl"] = "http://finance.internal:8091"
            })
            .Build();
        return middleware.InvokeAsync(
            context,
            new TestHttpClientFactory(handler),
            configuration,
            NullLogger<FinanceGatewayMiddleware>.Instance);
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public string? UserId { get; private set; }
        public string? UserRole { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            UserId = request.Headers.GetValues("X-User-Id").Single();
            UserRole = request.Headers.GetValues("X-User-Role").Single();
            return Task.FromResult(respond(request));
        }
    }
}
