using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ApiApplication.Middleware
{
    public class ExceptionMiddleware
    {
        private static readonly JsonSerializerOptions JsonOpts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _log;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log)
        {
            _next = next;
            _log = log;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await _next(ctx);
            }
            catch (Exception ex)
            {
                await WriteErrorAsync(ctx, ex);
            }
        }

        private async Task WriteErrorAsync(HttpContext ctx, Exception ex)
        {
            _log.LogError(ex, "Unhandled error on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

            if (ctx.Response.HasStarted)
            {
                throw ex;
            }

            var (status, message) = Map(ex);

            ctx.Response.Clear();
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode = (int)status;

            await ctx.Response.WriteAsync(
                JsonSerializer.Serialize(new { message }, JsonOpts));
        }

        private static (HttpStatusCode status, string message) Map(Exception ex)
        {
            switch (ex)
            {
                case ArgumentException argEx:
                    var notFound = argEx.Message.Contains("couldn't find", StringComparison.OrdinalIgnoreCase);
                    return (
                        notFound ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest,
                        argEx.Message);

                case InvalidOperationException opEx:
                    var conflict =
                        opEx.Message.Contains("already", StringComparison.OrdinalIgnoreCase) ||
                        opEx.Message.Contains("expired", StringComparison.OrdinalIgnoreCase) ||
                        opEx.Message.Contains("reserved", StringComparison.OrdinalIgnoreCase) ||
                        opEx.Message.Contains("sold", StringComparison.OrdinalIgnoreCase);
                    return (
                        conflict ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest,
                        opEx.Message);

                default:
                    return (
                        HttpStatusCode.InternalServerError,
                        "Something went wrong Please try again");
            }
        }
    }
}
