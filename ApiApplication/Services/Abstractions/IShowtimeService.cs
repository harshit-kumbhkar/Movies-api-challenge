using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Models.Requests;
using ApiApplication.Models.Responses;

namespace ApiApplication.Services.Abstractions
{
    public interface IShowtimeService
    {
        Task<ShowtimeResponse> CreateAsync(CreateShowtimeRequest request, CancellationToken cancel);
        Task<ShowtimeResponse> GetByIdAsync(int id, CancellationToken cancel);
    }
}
