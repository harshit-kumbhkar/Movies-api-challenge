using System;
using ApiApplication.Configuration;
using ApiApplication.Database;
using ApiApplication.Database.Repositories;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Middleware;
using ApiApplication.Services;
using ApiApplication.Services.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace ApiApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ProvidedApiOptions>(Configuration.GetSection(ProvidedApiOptions.SectionName));

            services.AddTransient<IShowtimesRepository, ShowtimesRepository>();
            services.AddTransient<ITicketsRepository, TicketsRepository>();
            services.AddTransient<IAuditoriumsRepository, AuditoriumsRepository>();

            var redisConn = RedisConfig.ResolveConnectionString(Configuration);
            var redisOpts = RedisConfig.Create(redisConn);

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisOpts));
            services.AddSingleton<ISeatHoldStore, RedisSeatHoldStore>();
            services.AddTransient<IShowtimeService, ShowtimeService>();
            services.AddTransient<IReservationService, ReservationService>();

            services.AddSingleton<ApiClientGrpc>();
            services.AddHttpClient<HttpMoviesApiClient>((_, client) =>
            {
                var opts = Configuration.GetSection(ProvidedApiOptions.SectionName).Get<ProvidedApiOptions>()
                    ?? new ProvidedApiOptions();
                client.BaseAddress = new Uri(opts.HttpBaseUrl.TrimEnd('/') + "/");
                client.DefaultRequestHeaders.Add("X-Apikey", opts.ApiKey);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            services.AddTransient<IMoviesApiClient, CachedMoviesApiClient>();

            services.AddStackExchangeRedisCache(opts =>
            {
                opts.ConfigurationOptions = redisOpts;
            });

            services.AddDbContext<CinemaContext>(opts =>
            {
                opts.UseInMemoryDatabase("CinemaDb")
                    .EnableSensitiveDataLogging()
                    .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            services.AddControllers();
            services.AddSwaggerGen();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Catch all errors and return JSON only — never expose stack traces
            app.UseMiddleware<ExceptionMiddleware>();
            app.UseMiddleware<RequestTimingMiddleware>();

            // Swagger enabled so Railway / local demos can open /swagger
            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            SampleData.Initialize(app);
        }
    }
}
