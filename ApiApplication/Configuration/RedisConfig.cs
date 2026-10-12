using System;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace ApiApplication.Configuration
{
    public static class RedisConfig
    {
        /// <summary>
        /// Builds Redis options from ConnectionStrings:Redis or Railway REDIS_URL
        /// (redis://default:password@host:6379).
        /// </summary>
        public static ConfigurationOptions Create(string connection)
        {
            if (string.IsNullOrWhiteSpace(connection))
            {
                throw new InvalidOperationException(
                    "Redis connection is missing. Set ConnectionStrings__Redis or REDIS_URL");
            }

            connection = connection.Trim().Trim('"');
            ConfigurationOptions opts;

            if (connection.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                connection.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(connection);
                var port = uri.Port > 0 ? uri.Port : 6379;
                opts = new ConfigurationOptions
                {
                    EndPoints = { { uri.Host, port } },
                    Ssl = connection.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase)
                };

                if (!string.IsNullOrEmpty(uri.UserInfo))
                {
                    var parts = uri.UserInfo.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        opts.User = Uri.UnescapeDataString(parts[0]);
                        opts.Password = Uri.UnescapeDataString(parts[1]);
                    }
                    else
                    {
                        opts.Password = Uri.UnescapeDataString(parts[0]);
                    }
                }
            }
            else
            {
                opts = ConfigurationOptions.Parse(connection);
            }

            opts.AbortOnConnectFail = false;
            opts.ConnectRetry = 5;
            opts.ConnectTimeout = 15000;
            opts.SyncTimeout = 10000;
            opts.AsyncTimeout = 10000;
            return opts;
        }

        public static string ResolveConnectionString(IConfiguration config)
        {
            // Prefer REDIS_URL (Railway) — avoid double-parsing bad ConnectionStrings
            return config["REDIS_URL"]
                ?? config.GetConnectionString("Redis")
                ?? config["Redis__ConnectionString"]
                ?? string.Empty;
        }
    }
}
