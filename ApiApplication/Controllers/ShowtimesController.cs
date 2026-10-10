using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Models.Requests;
using ApiApplication.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiApplication.Controllers
{
    [ApiController]
    [Route("api/showtimes")]
    public class ShowtimesController : ControllerBase
    {
        private readonly IShowtimeService _showtimes;
        private readonly IReservationService _reservations;

        public ShowtimesController(IShowtimeService showtimes, IReservationService reservations)
        {
            _showtimes = showtimes;
            _reservations = reservations;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateShowtimeRequest request, CancellationToken cancel)
        {
            var result = await _showtimes.CreateAsync(request, cancel);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancel)
        {
            var result = await _showtimes.GetByIdAsync(id, cancel);
            return Ok(result);
        }

        [HttpPost("{showtimeId:int}/reservations")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reserve(
            int showtimeId,
            [FromBody] ReserveSeatsRequest request,
            CancellationToken cancel)
        {
            var result = await _reservations.ReserveAsync(showtimeId, request, cancel);
            return CreatedAtAction(
                nameof(ReservationsController.Confirm),
                "Reservations",
                new { reservationId = result.ReservationId },
                result);
        }
    }
}
