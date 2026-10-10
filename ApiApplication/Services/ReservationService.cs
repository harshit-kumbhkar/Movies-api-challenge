using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Models.Requests;
using ApiApplication.Models.Responses;
using ApiApplication.Services.Abstractions;

namespace ApiApplication.Services
{
    public class ReservationService : IReservationService
    {
        public static readonly TimeSpan HoldTime = TimeSpan.FromMinutes(10);

        private readonly IShowtimesRepository _showtimes;
        private readonly IAuditoriumsRepository _auditoriums;
        private readonly ITicketsRepository _tickets;
        private readonly ISeatHoldStore _seatHolds;

        public ReservationService(
            IShowtimesRepository showtimes,
            IAuditoriumsRepository auditoriums,
            ITicketsRepository tickets,
            ISeatHoldStore seatHolds)
        {
            _showtimes = showtimes;
            _auditoriums = auditoriums;
            _tickets = tickets;
            _seatHolds = seatHolds;
        }

        public async Task<ReservationResponse> ReserveAsync(
            int showtimeId,
            ReserveSeatsRequest request,
            CancellationToken cancel)
        {
            if (request?.Seats == null || request.Seats.Count == 0)
            {
                throw new ArgumentException("Please pick at least one seat");
            }

            if (!AreContiguous(request.Seats))
            {
                throw new ArgumentException("Please choose seats that sit next to each other in the same row");
            }

            var showtime = await _showtimes.GetWithMoviesByIdAsync(showtimeId, cancel);
            if (showtime == null)
            {
                throw new ArgumentException($"We couldn't find showtime {showtimeId}");
            }

            var hall = await _auditoriums.GetAsync(showtime.AuditoriumId, cancel);
            if (hall == null)
            {
                throw new ArgumentException($"We couldn't find auditorium {showtime.AuditoriumId}");
            }

            var seats = new List<SeatEntity>();
            foreach (var req in request.Seats)
            {
                var seat = hall.Seats.FirstOrDefault(s =>
                    s.Row == req.Row && s.SeatNumber == req.SeatNumber);

                if (seat == null)
                {
                    throw new ArgumentException(
                        $"Seat {req.Row}-{req.SeatNumber} isn't available in auditorium {hall.Id}");
                }

                seats.Add(seat);
            }

            var seatKeys = seats
                .Select(s => (s.Row, s.SeatNumber))
                .ToList();

            // Claim only these seats in Redis (other seats stay free for parallel requests)
            var holdId = Guid.NewGuid().ToString("N");
            var held = await _seatHolds.TryHoldAsync(showtimeId, seatKeys, holdId, HoldTime, cancel);
            if (!held)
            {
                throw new InvalidOperationException("One or more seats are already reserved");
            }

            try
            {
                // Extra safety with DB tickets
                var tickets = await _tickets.GetEnrichedAsync(showtimeId, cancel);
                EnsureSeatsFree(seats, tickets);

                var ticket = await _tickets.CreateAsync(showtime, seats, cancel);
                ticket = await _tickets.GetAsync(ticket.Id, cancel);
                return Map(ticket);
            }
            catch
            {
                await _seatHolds.ReleaseAsync(showtimeId, seatKeys, cancel);
                throw;
            }
        }

        public async Task<ReservationResponse> ConfirmAsync(Guid reservationId, CancellationToken cancel)
        {
            var ticket = await _tickets.GetAsync(reservationId, cancel);
            if (ticket == null)
            {
                throw new ArgumentException($"We couldn't find reservation {reservationId}");
            }

            if (ticket.Paid)
            {
                throw new InvalidOperationException("This reservation is already paid");
            }

            if (IsExpired(ticket))
            {
                throw new InvalidOperationException("This reservation has expired and can't be confirmed");
            }

            var tickets = await _tickets.GetEnrichedAsync(ticket.ShowtimeId, cancel);
            var soldClash = tickets
                .Where(t => t.Paid && t.Id != ticket.Id)
                .SelectMany(t => t.Seats)
                .Where(seat => ticket.Seats.Any(s => s.Row == seat.Row && s.SeatNumber == seat.SeatNumber))
                .ToList();

            if (soldClash.Count > 0)
            {
                throw new InvalidOperationException("Some of these seats were already sold");
            }

            ticket = await _tickets.ConfirmPaymentAsync(ticket, cancel);
            ticket = await _tickets.GetAsync(ticket.Id, cancel);

            var seatKeys = ticket.Seats
                .Select(s => (s.Row, s.SeatNumber))
                .ToList();
            await _seatHolds.MarkSoldAsync(ticket.ShowtimeId, seatKeys, ticket.Id.ToString("N"), cancel);

            return Map(ticket);
        }

        private static void EnsureSeatsFree(
            IEnumerable<SeatEntity> seats,
            IEnumerable<TicketEntity> tickets)
        {
            var blocked = tickets
                .Where(BlocksSeat)
                .SelectMany(t => t.Seats)
                .ToList();

            foreach (var seat in seats)
            {
                var taken = blocked.Any(b => b.Row == seat.Row && b.SeatNumber == seat.SeatNumber);
                if (!taken)
                {
                    continue;
                }

                var sold = tickets.Any(t =>
                    t.Paid &&
                    t.Seats.Any(s => s.Row == seat.Row && s.SeatNumber == seat.SeatNumber));

                if (sold)
                {
                    throw new InvalidOperationException(
                        $"Seat {seat.Row}-{seat.SeatNumber} is already sold");
                }

                throw new InvalidOperationException(
                    $"Seat {seat.Row}-{seat.SeatNumber} is already reserved");
            }
        }

        private static bool BlocksSeat(TicketEntity ticket)
        {
            return ticket.Paid || !IsExpired(ticket);
        }

        private static bool IsExpired(TicketEntity ticket)
        {
            return DateTime.Now - ticket.CreatedTime >= HoldTime;
        }

        private static bool AreContiguous(IEnumerable<SeatRequest> seats)
        {
            var ordered = seats.OrderBy(s => s.Row).ThenBy(s => s.SeatNumber).ToList();
            if (ordered.Count == 0)
            {
                return false;
            }

            if (ordered.Select(s => s.Row).Distinct().Count() != 1)
            {
                return false;
            }

            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].SeatNumber != ordered[i - 1].SeatNumber + 1)
                {
                    return false;
                }
            }

            return true;
        }

        private static ReservationResponse Map(TicketEntity ticket)
        {
            return new ReservationResponse
            {
                ReservationId = ticket.Id,
                NumberOfSeats = ticket.Seats?.Count ?? 0,
                AuditoriumId = ticket.Showtime?.AuditoriumId ?? 0,
                CreatedTime = ticket.CreatedTime,
                ExpiresAt = ticket.CreatedTime.Add(HoldTime),
                Paid = ticket.Paid,
                Movie = ticket.Showtime?.Movie == null
                    ? null
                    : new MovieResponse
                    {
                        Id = ticket.Showtime.Movie.Id,
                        Title = ticket.Showtime.Movie.Title,
                        ImdbId = ticket.Showtime.Movie.ImdbId,
                        Stars = ticket.Showtime.Movie.Stars,
                        ReleaseDate = ticket.Showtime.Movie.ReleaseDate
                    },
                Seats = ticket.Seats?.Select(s => new SeatResponse
                {
                    Row = s.Row,
                    SeatNumber = s.SeatNumber
                }) ?? Enumerable.Empty<SeatResponse>()
            };
        }
    }
}
