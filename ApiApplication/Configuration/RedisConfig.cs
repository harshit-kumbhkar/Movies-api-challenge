using System;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace ApiApplication.Configuration
{
    public static class RedisConfig
    {
        /// <summary>
        /// Builds a Redis config from ConnectionStrings:Redis or Railway REDIS_URL.
        /// </summary>
        public static ConfigurationOptions Create(string connection)
        {
            if (string.IsNullOrWhiteSpace(connection))
            {
                throw new InvalidOperationException(
                    "Redis connection is missing. Set ConnectionStrings__Redis or REDIS_URL");
            }

            var opts = ConfigurationOptions.Parse(connection.Trim());
            opts.AbortOnConnectFail = false;
            opts.ConnectRetry = 3;
            opts.ConnectTimeout = 10000;
            return opts;
        }

        public static string ResolveConnectionString(IConfiguration config)
        {
            return config.GetConnectionString("Redis")
                ?? config["REDIS_URL"]
                ?? config["Redis__ConnectionString"]
                ?? string.Empty;
        }
    }
}
