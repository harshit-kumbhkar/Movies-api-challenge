using System;
using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Models.Requests;
using ApiApplication.Models.Responses;

namespace ApiApplication.Services.Abstractions
{
    public interface IReservationService
    {
        Task<ReservationResponse> ReserveAsync(int showtimeId, ReserveSeatsRequest request, CancellationToken cancel);
        Task<ReservationResponse> ConfirmAsync(Guid revId, CancellationToken cancel);
    }
}
