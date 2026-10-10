using System;
using System.Threading;
using System.Threading.Tasks;
using ApiApplication.Database.Entities;
using ApiApplication.Database.Repositories.Abstractions;
using ApiApplication.Models.Requests;
using ApiApplication.Models.Responses;
using ApiApplication.Services.Abstractions;

namespace ApiApplication.Services
{
    public class ShowtimeService : IShowtimeService
    {
        private readonly IShowtimesRepository _showtimes;
        private readonly IAuditoriumsRepository _auditoriums;
        private readonly IMoviesApiClient _movies;

        public ShowtimeService(
            IShowtimesRepository showtimes,
            IAuditoriumsRepository auditoriums,
            IMoviesApiClient movies)
        {
            _showtimes = showtimes;
            _auditoriums = auditoriums;
            _movies = movies;
        }

        public async Task<ShowtimeResponse> CreateAsync(CreateShowtimeRequest request, CancellationToken cancel)
        {
            var hall = await _auditoriums.GetAsync(request.AuditoriumId, cancel);
            if (hall == null)
            {
                throw new ArgumentException($"We couldn't find auditorium {request.AuditoriumId}");
            }

            var movie = await _movies.GetByIdAsync(request.MovieId, cancel);

            var showtime = new ShowtimeEntity
            {
                AuditoriumId = request.AuditoriumId,
                SessionDate = request.SessionDate,
                Movie = new MovieEntity
                {
                    Title = movie.Title,
                    ImdbId = movie.Id,
                    Stars = movie.Crew ?? string.Empty,
                    ReleaseDate = movie.GetReleaseDate()
                }
            };

            var created = await _showtimes.CreateShowtime(showtime, cancel);
            return Map(created);
        }

        public async Task<ShowtimeResponse> GetByIdAsync(int id, CancellationToken cancel)
        {
            var showtime = await _showtimes.GetWithMoviesByIdAsync(id, cancel);
            if (showtime == null)
            {
                throw new ArgumentException($"We couldn't find showtime {id}");
            }

            return Map(showtime);
        }

        private static ShowtimeResponse Map(ShowtimeEntity showtime)
        {
            return new ShowtimeResponse
            {
                Id = showtime.Id,
                AuditoriumId = showtime.AuditoriumId,
                SessionDate = showtime.SessionDate,
                Movie = showtime.Movie == null
                    ? null
                    : new MovieResponse
                    {
                        Id = showtime.Movie.Id,
                        Title = showtime.Movie.Title,
                        ImdbId = showtime.Movie.ImdbId,
                        Stars = showtime.Movie.Stars,
                        ReleaseDate = showtime.Movie.ReleaseDate
                    }
            };
        }
    }
}
