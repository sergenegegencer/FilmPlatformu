namespace FilmPlatformu.Models
{
    public class Review
    {
        public int Id { get; set; }
        public int FilmId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime Date { get; set; }

        public Film? Film { get; set; }
    }
}
