using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Models;

namespace ApiApplication.Services.Abstractions
{
    public interface IMoviesApiClient
    {
        Task<ProvidedMovie> GetByIdAsync(string movieId, CancellationToken cancel);
    }
}
