using System.Net;
using System.Text.Json;
using Oficina.Execucao.Domain.Exceptions;

namespace Oficina.Execucao.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (DomainException ex)
        {
            _logger.LogWarning("Domain: {Msg} | TraceId: {TraceId}", ex.Message, context.TraceIdentifier);
            await Write(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado | TraceId: {TraceId}", context.TraceIdentifier);
            await Write(context, HttpStatusCode.InternalServerError, "Ocorreu um erro interno.");
        }
    }

    private static async Task Write(HttpContext ctx, HttpStatusCode code, string msg)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = (int)code;
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(new { error = msg, traceId = ctx.TraceIdentifier }, JsonOpts));
    }
}
