using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Services.Abstractions;
using StackExchange.Redis;

namespace ApiApplication.Services
{
    /// <summary>
    /// Per-seat locks in Redis so two users can reserve different seats at once,
    /// but the same seat can only be claimed by one request.
    /// Key: cinema:seat:{showtimeId}:{row}:{seatNumber}
    /// </summary>
    public class RedisSeatHoldStore : ISeatHoldStore
    {
        private readonly IConnectionMultiplexer _redis;

        public RedisSeatHoldStore(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public async Task<bool> TryHoldAsync(
            int showtimeId,
            IReadOnlyList<(short Row, short SeatNumber)> seats,
            string holdId,
            TimeSpan ttl,
            CancellationToken cancel)
        {
            var db = _redis.GetDatabase();
            var claimed = new List<RedisKey>();
            var value = $"hold:{holdId}";

            foreach (var seat in Order(seats))
            {
                cancel.ThrowIfCancellationRequested();
                var key = Key(showtimeId, seat.Row, seat.SeatNumber);
                var ok = await db.StringSetAsync(key, value, ttl, When.NotExists);
                if (!ok)
                {
                    if (claimed.Count > 0)
                    {
                        await db.KeyDeleteAsync(claimed.ToArray());
                    }

                    return false;
                }

                claimed.Add(key);
            }

            return true;
        }

        public async Task MarkSoldAsync(
            int showtimeId,
            IReadOnlyList<(short Row, short SeatNumber)> seats,
            string holdId,
            CancellationToken cancel)
        {
            var db = _redis.GetDatabase();
            var value = $"sold:{holdId}";

            foreach (var seat in Order(seats))
            {
                cancel.ThrowIfCancellationRequested();
                var key = Key(showtimeId, seat.Row, seat.SeatNumber);
                // Keep sold seats blocked (no short TTL)
                await db.StringSetAsync(key, value);
            }
        }

        public async Task ReleaseAsync(
            int showtimeId,
            IReadOnlyList<(short Row, short SeatNumber)> seats,
            CancellationToken cancel)
        {
            var db = _redis.GetDatabase();
            var keys = Order(seats)
                .Select(s => Key(showtimeId, s.Row, s.SeatNumber))
                .ToArray();

            if (keys.Length > 0)
            {
                await db.KeyDeleteAsync(keys);
            }
        }

        private static RedisKey Key(int showtimeId, short row, short seat) =>
            $"cinema:seat:{showtimeId}:{row}:{seat}";

        private static IEnumerable<(short Row, short SeatNumber)> Order(
            IEnumerable<(short Row, short SeatNumber)> seats) =>
            seats.OrderBy(s => s.Row).ThenBy(s => s.SeatNumber);
    }
}
