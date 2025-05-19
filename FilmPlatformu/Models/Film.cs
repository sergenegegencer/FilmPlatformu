namespace FilmPlatformu.Models
{
    public class Film
    {
        public int Id { get; set; }
        public int TMDBId { get; set; }
        public string Title { get; set; }
        public string PosterUrl { get; set; }
        public string? Overview { get; set; }
        public string? ReleaseDate { get; set; }
        public int? Runtime { get; set; }
        public string? OriginalLanguage { get; set; }
        public string? Genres { get; set; }
        public ICollection<Review>? Reviews { get; set; }
    }
}
