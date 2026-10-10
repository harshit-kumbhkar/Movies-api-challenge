using ApiApplication.Database.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;
using System.Linq.Expressions;
using ApiApplication.Database.Repositories.Abstractions;

namespace ApiApplication.Database.Repositories
{
    public class ShowtimesRepository : IShowtimesRepository
    {
        private readonly CinemaContext _context;

        public ShowtimesRepository(CinemaContext context)
        {
            _context = context;
        }

        public async Task<ShowtimeEntity> GetWithMoviesByIdAsync(int id, CancellationToken cancel)
        {
            return await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _context.Showtimes.Include(x => x.Movie),
                x => x.Id == id,
                cancel);
        }

        public async Task<ShowtimeEntity> GetWithTicketsByIdAsync(int id, CancellationToken cancel)
        {
            return await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                _context.Showtimes.Include(x => x.Tickets),
                x => x.Id == id,
                cancel);
        }

        public async Task<IEnumerable<ShowtimeEntity>> GetAllAsync(Expression<Func<ShowtimeEntity, bool>> filter, CancellationToken cancel)
        {
            if (filter == null)
            {
                return await EntityFrameworkQueryableExtensions.ToListAsync(
                    _context.Showtimes.Include(x => x.Movie),
                    cancel);
            }
            return await EntityFrameworkQueryableExtensions.ToListAsync(
                _context.Showtimes.Include(x => x.Movie).Where(filter),
                cancel);
        }

        public async Task<ShowtimeEntity> CreateShowtime(ShowtimeEntity showtimeEntity, CancellationToken cancel)
        {
            var showtime = await _context.Showtimes.AddAsync(showtimeEntity, cancel);
            await _context.SaveChangesAsync(cancel);
            return showtime.Entity;
        }
    }
}
