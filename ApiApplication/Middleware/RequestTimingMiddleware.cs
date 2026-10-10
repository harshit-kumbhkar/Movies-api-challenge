using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ApiApplication.Middleware
{
    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimingMiddleware> _log;

        public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> log)
        {
            _next = next;
            _log = log;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                await _next(ctx);
            }
            finally
            {
                sw.Stop();
                _log.LogInformation(
                    "{Method} {Path} finished in {ElapsedMilliseconds} ms (status {StatusCode})",
                    ctx.Request.Method,
                    ctx.Request.Path,
                    sw.ElapsedMilliseconds,
                    ctx.Response.StatusCode);
            }
        }
    }
}
