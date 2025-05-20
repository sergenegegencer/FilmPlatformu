namespace FilmPlatformu.Models
{
    public class UserPanelViewModel
    {
        public List<Film> Films { get; set; } = new();
        public string? Query { get; set; }
        public string? FilterGenre { get; set; }
        public string? FilterYear { get; set; }
        public string? FilterLanguage { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }

}
