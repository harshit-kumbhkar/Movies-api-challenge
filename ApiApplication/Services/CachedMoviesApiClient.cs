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

            try
            {
                var movie = await _grpc.GetByIdAsync(movieId, cancel);
                await CacheAsync(movieId, movie, cancel);
                return movie;
            }
            catch (Exception ex)
            {
                lastErr = ex;
                _log.LogWarning(ex, "gRPC lookup failed for movie {Id}, trying HTTP", movieId);
            }

            try
            {
                var movie = await _http.GetByIdAsync(movieId, cancel);
                await CacheAsync(movieId, movie, cancel);
                return movie;
            }
            catch (Exception ex)
            {
                lastErr = ex;
                _log.LogWarning(ex, "HTTP lookup failed for movie {Id}, trying Redis cache", movieId);
            }

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

            throw new InvalidOperationException(
                $"We couldn't load movie {movieId} right now — please try again",
                lastErr);
        }

        private async Task CacheAsync(string movieId, ProvidedMovie movie, CancellationToken cancel)
        {
            var payload = JsonSerializer.Serialize(movie);
            var opts = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_opts.CacheExpirationMinutes)
            };

            await _cache.SetStringAsync(CacheKey(movieId), payload, opts, cancel);
        }

        private static string CacheKey(string movieId) => $"provided-movie:{movieId}";

    }

}
