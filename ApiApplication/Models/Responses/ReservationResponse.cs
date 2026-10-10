using System;
using System.Collections.Generic;

namespace ApiApplication.Models.Responses
{
    public class ReservationResponse
    {
        public Guid ReservationId { get; set; }
        public int NumberOfSeats { get; set; }
        public int AuditoriumId { get; set; }
        public MovieResponse Movie { get; set; }
        public IEnumerable<SeatResponse> Seats { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Paid { get; set; }
    }

    public class SeatResponse
    {
        public short Row { get; set; }
        public short SeatNumber { get; set; }
    }
}
