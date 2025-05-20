namespace FilmPlatformu.Models
{
    public class AdminPanelViewModel
    {
        public List<Review>? AllReviews { get; set; }
        public List<Film>? PopularFilms { get; set; }
        public int TotalUsers { get; set; }
        public int TotalReviews { get; set; }
        public string? FilterUsername { get; set; }
        public string? FilterFilmTitle { get; set; }
        public string SelectedTab { get; set; } = "summary";
        public string? FilterGenre { get; set; }
        public string? FilterYear { get; set; }
        public string? FilterLanguage { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? Query { get; set; }
    }

}
