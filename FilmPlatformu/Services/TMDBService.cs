using System.Net.Http;
using System.Net.Http.Json;
using FilmPlatformu.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

public class TMDBService
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public TMDBService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["TMDB:ApiKey"];
    }

    public async Task<Film> GetFilmByIdAsync(int tmdbId)
    {
        var url = $"https://api.themoviedb.org/3/movie/{tmdbId}?api_key={_apiKey}&language=tr-TR";
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"TMDB film bulunamadı. ID: {tmdbId}");
        var json = JObject.Parse(await response.Content.ReadAsStringAsync());

        var genres = json["genres"]?
        .Select(g => g.Value<string>("name"))
        .Where(n => !string.IsNullOrEmpty(n)) ?? Enumerable.Empty<string>();

        return new Film
        {
            TMDBId = tmdbId,
            Title = json.Value<string>("title") ?? "Bilinmiyor",
            PosterUrl = "https://image.tmdb.org/t/p/w500" + (json.Value<string>("poster_path") ?? ""),
            Overview = json.Value<string>("overview"),
            ReleaseDate = json.Value<string>("release_date"),
            Runtime = json.Value<int?>("runtime"),
            OriginalLanguage = json.Value<string>("original_language"),
            Genres = string.Join(", ", genres)
        };
    }


    public async Task<List<Film>> SearchMoviesAsync(string query)
    {
        var url = $"https://api.themoviedb.org/3/search/movie?api_key={_apiKey}&query={Uri.EscapeDataString(query)}";

        var response = await _httpClient.GetStringAsync(url);
        var json = JObject.Parse(response);
        var results = json["results"];

        var films = new List<Film>();

        foreach (var item in results!)
        {
            films.Add(new Film
            {
                Title = item.Value<string>("title") ?? "Bilinmiyor",
                PosterUrl = "https://image.tmdb.org/t/p/w500" + (item.Value<string>("poster_path") ?? ""),
                TMDBId = item.Value<int>("id")
            });
        }

        return films;
    }
}
