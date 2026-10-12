using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Configuration;
using ApiApplication.Models;
using ApiApplication.Services.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiApplication.Services
{
    public class CachedMoviesApiClient : IMoviesApiClient
    {
        private readonly ApiClientGrpc _grpc;
        private readonly HttpMoviesApiClient _http;
        private readonly IDistributedCache _cache;
        private readonly ProvidedApiOptions _opts;
        private readonly ILogger<CachedMoviesApiClient> _log;

        public CachedMoviesApiClient(
            ApiClientGrpc grpc,
            HttpMoviesApiClient http,
            IDistributedCache cache,
            IOptions<ProvidedApiOptions> opts,
            ILogger<CachedMoviesApiClient> log)
        {
            _grpc = grpc;
            _http = http;
            _cache = cache;
            _opts = opts.Value;
            _log = log;
        }

        public async Task<ProvidedMovie> GetByIdAsync(string movieId, CancellationToken cancel)
        {
            Exception lastErr = null;

            // Prefer HTTP on Railway (public HTTPS URL works; gRPC often does not)
            try
            {
                var movie = await _http.GetByIdAsync(movieId, cancel);
                await TryCacheAsync(movieId, movie, cancel);
                return movie;
            }
            catch (Exception ex)
            {
                lastErr = ex;
                _log.LogWarning(ex, "HTTP lookup failed for movie {Id}, trying gRPC", movieId);
            }

            if (IsGrpcConfigured())
            {
                try
                {
                    var movie = await _grpc.GetByIdAsync(movieId, cancel);
                    await TryCacheAsync(movieId, movie, cancel);
                    return movie;
                }
                catch (Exception ex)
                {
                    lastErr = ex;
                    _log.LogWarning(ex, "gRPC lookup failed for movie {Id}, trying Redis cache", movieId);
                }
            }

            try
            {
                var cached = await _cache.GetStringAsync(CacheKey(movieId), cancel);
                if (!string.IsNullOrWhiteSpace(cached))
                {
                    var movie = JsonSerializer.Deserialize<ProvidedMovie>(cached);
                    if (movie != null)
                    {
                        _log.LogInformation("Served movie {Id} from Redis cache", movieId);
                        return movie;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Redis cache read failed for movie {Id}", movieId);
            }

            throw new InvalidOperationException(
                $"We couldn't load movie {movieId} right now — please try again",
                lastErr);
        }

        private bool IsGrpcConfigured()
        {
            var addr = _opts.GrpcAddress;
            if (string.IsNullOrWhiteSpace(addr))
            {
                return false;
            }

            if (addr.Contains("unused", StringComparison.OrdinalIgnoreCase) ||
                addr.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return Uri.TryCreate(addr, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private async Task TryCacheAsync(string movieId, ProvidedMovie movie, CancellationToken cancel)
        {
            try
            {
                var payload = JsonSerializer.Serialize(movie);
                var opts = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_opts.CacheExpirationMinutes)
                };
                await _cache.SetStringAsync(CacheKey(movieId), payload, opts, cancel);
            }
            catch (Exception ex)
            {
                // Movie was fetched successfully — don't fail the request if Redis is down
                _log.LogWarning(ex, "Redis cache write failed for movie {Id}", movieId);
            }
        }

        private static string CacheKey(string movieId) => $"provided-movie:{movieId}";
    }
}
