using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ApiApplication.Services.Abstractions
{
    public interface ISeatHoldStore
    {
        /// <summary>
        /// Tries to claim seats in Redis (SET NX). Returns false if any seat is already held/sold.
        /// </summary>
        Task<bool> TryHoldAsync(
            int showtimeId,
            IReadOnlyList<(short Row, short SeatNumber)> seats,
            string holdId,
            TimeSpan ttl,
            CancellationToken cancel);

        /// <summary>
        /// Marks seats as sold (kept in Redis so they can't be reserved again).
        /// </summary>
        Task MarkSoldAsync(
            int showtimeId,
            IReadOnlyList<(short Row, short SeatNumber)> seats,
            string holdId,
            CancellationToken cancel);

        /// <summary>
        /// Drops Redis holds (e.g. if DB create failed after a successful claim).
        /// </summary>
        Task ReleaseAsync(
            int showtimeId,
            IReadOnlyList<(short Row, short SeatNumber)> seats,
            CancellationToken cancel);
    }
}
