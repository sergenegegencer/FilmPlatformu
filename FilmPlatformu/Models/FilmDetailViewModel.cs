namespace FilmPlatformu.Models
{
    public class FilmDetailViewModel
    {
        public Film Film { get; set; } = new();
        public bool IsInWatchlist { get; set; }
    }

}
