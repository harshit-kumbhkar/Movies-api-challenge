using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Configuration;
using ApiApplication.Models;
using ApiApplication.Services.Abstractions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProtoDefinitions;

namespace ApiApplication
{
    public class ApiClientGrpc : IMoviesApiClient, IDisposable
    {
        private readonly ProvidedApiOptions _opts;
        private readonly ILogger<ApiClientGrpc> _log;
        private readonly GrpcChannel _channel;
        private readonly MoviesApi.MoviesApiClient _client;

        public ApiClientGrpc(IOptions<ProvidedApiOptions> opts, ILogger<ApiClientGrpc> log)
        {
            _opts = opts.Value;
            _log = log;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            _channel = GrpcChannel.ForAddress(_opts.GrpcAddress, new GrpcChannelOptions
            {
                HttpHandler = handler
            });
            _client = new MoviesApi.MoviesApiClient(_channel);
        }

        public async Task<ProvidedMovie> GetByIdAsync(string movieId, CancellationToken cancel)
        {
            var headers = CreateHeaders();
            var resp = await _client.GetByIdAsync(
                new IdRequest { Id = movieId },
                headers,
                cancellationToken: cancel);

            if (!resp.Success)
            {
                var msg = resp.Exceptions.Count > 0
                    ? resp.Exceptions[0].Message.TrimEnd('.')
                    : $"Couldn't fetch movie {movieId} via gRPC";
                throw new InvalidOperationException(msg);
            }

            if (!resp.Data.TryUnpack<showResponse>(out var show) || show == null)
            {
                throw new InvalidOperationException($"Couldn't read movie details for {movieId}");
            }

            _log.LogInformation("Fetched movie {MovieId} via gRPC", movieId);
            return Map(show);
        }

        public async Task<showListResponse> GetAllAsync(CancellationToken cancel = default)
        {
            var resp = await _client.GetAllAsync(new Empty(), CreateHeaders(), cancellationToken: cancel);
            if (!resp.Success || !resp.Data.TryUnpack<showListResponse>(out var data))
            {
                throw new InvalidOperationException("Couldn't fetch the movie list via gRPC");
            }

            return data;
        }

        private Metadata CreateHeaders()
        {
            return new Metadata
            {
                { "X-Apikey", _opts.ApiKey }
            };
        }

        private static ProvidedMovie Map(showResponse show)
        {
            return new ProvidedMovie
            {
                Id = show.Id,
                Title = show.Title,
                FullTitle = show.FullTitle,
                Year = show.Year,
                Crew = show.Crew
            };
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
