using FilmPlatformu.Models;
using Microsoft.AspNetCore.Identity;

public class Watchlist
{
    public int Id { get; set; }

    public int FilmId { get; set; }
    public Film Film { get; set; }

    public string UserId { get; set; } = null!;
    public IdentityUser User { get; set; } = null!;
}
