using System;
using System.ComponentModel.DataAnnotations;

namespace ApiApplication.Models.Requests
{
    public class CreateShowtimeRequest
    {
        [Required]
        public string MovieId { get; set; }

        [Range(1, int.MaxValue)]
        public int AuditoriumId { get; set; }

        [Required]
        public DateTime SessionDate { get; set; }
    }
}
