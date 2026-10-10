using System;
using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApiApplication.Controllers
{
    [ApiController]
    [Route("api/reservations")]
    public class ReservationsController : ControllerBase
    {
        private readonly IReservationService _reservations;

        public ReservationsController(IReservationService reservations)
        {
            _reservations = reservations;
        }

        [HttpPost("{reservationId:guid}/confirm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Confirm(Guid reservationId, CancellationToken cancel)
        {
            var result = await _reservations.ConfirmAsync(reservationId, cancel);
            return Ok(result);
        }
    }
}
