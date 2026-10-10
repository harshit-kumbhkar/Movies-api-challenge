using System;

namespace ApiApplication.Models
{
    public class ProvidedMovie
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string FullTitle { get; set; }
        public string Year { get; set; }
        public string Crew { get; set; }

        public DateTime GetReleaseDate()
        {
            if (int.TryParse(Year, out var year) && year > 0)
            {
                return new DateTime(year, 1, 1);
            }

            return DateTime.MinValue;
        }
    }
}
