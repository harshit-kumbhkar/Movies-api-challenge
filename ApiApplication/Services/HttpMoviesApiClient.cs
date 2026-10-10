using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Models;
using ApiApplication.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace ApiApplication.Services
{
    public class HttpMoviesApiClient : IMoviesApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<HttpMoviesApiClient> _log;

        public HttpMoviesApiClient(HttpClient http, ILogger<HttpMoviesApiClient> log)
        {
            _http = http;
            _log = log;
        }

        public async Task<ProvidedMovie> GetByIdAsync(string movieId, CancellationToken cancel)
        {
            using var resp = await _http.GetAsync($"v1/movies/{movieId}", cancel);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(cancel);
                throw new InvalidOperationException(
                    $"Couldn't fetch movie details (HTTP {(int)resp.StatusCode})");
            }

            var show = await resp.Content.ReadFromJsonAsync<HttpShowDto>(cancellationToken: cancel);
            if (show == null || string.IsNullOrWhiteSpace(show.Id))
            {
                throw new InvalidOperationException($"No details came back for movie {movieId}");
            }

            _log.LogInformation("Fetched movie {MovieId} via HTTP", movieId);

            return new ProvidedMovie
            {
                Id = show.Id,
                Title = show.Title,
                FullTitle = show.FullTitle,
                Year = show.Year,
                Crew = show.Crew
            };
        }

        private sealed class HttpShowDto
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("fullTitle")]
            public string FullTitle { get; set; }

            [JsonPropertyName("year")]
            public string Year { get; set; }

            [JsonPropertyName("crew")]
            public string Crew { get; set; }
        }
    }
}
