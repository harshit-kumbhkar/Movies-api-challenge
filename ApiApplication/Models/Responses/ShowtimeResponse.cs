using System;

namespace ApiApplication.Models.Responses
{
    public class ShowtimeResponse
    {
        public int Id { get; set; }
        public int AuditoriumId { get; set; }
        public DateTime SessionDate { get; set; }
        public MovieResponse Movie { get; set; }
    }

    public class MovieResponse
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ImdbId { get; set; }
        public string Stars { get; set; }
        public DateTime ReleaseDate { get; set; }
    }
}
