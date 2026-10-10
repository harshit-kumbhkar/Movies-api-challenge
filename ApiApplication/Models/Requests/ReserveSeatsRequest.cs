using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiApplication.Models.Requests
{
    public class ReserveSeatsRequest
    {
        [Required]
        [MinLength(1)]
        public List<SeatRequest> Seats { get; set; }
    }

    public class SeatRequest
    {
        [Range(1, short.MaxValue)]
        public short Row { get; set; }

        [Range(1, short.MaxValue)]
        public short SeatNumber { get; set; }
    }
}
