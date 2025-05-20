using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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

    public async Task<SearchResult> SearchMoviesAsync(string query, int page = 1)
    {
        var url = $"https://api.themoviedb.org/3/search/movie?api_key={_apiKey}&language=tr-TR&query={Uri.EscapeDataString(query)}&page={page}";

        var response = await _httpClient.GetStringAsync(url);
        var json = JObject.Parse(response);

        var results = json["results"];
        int totalPages = json["total_pages"]?.Value<int>() ?? 1;

        var genreMap = new Dictionary<int, string>
    {
        { 28, "Action" }, { 12, "Adventure" }, { 16, "Animation" },
        { 35, "Comedy" }, { 80, "Crime" }, { 99, "Documentary" },
        { 18, "Drama" }, { 10751, "Family" }, { 14, "Fantasy" },
        { 36, "History" }, { 27, "Horror" }, { 10402, "Music" },
        { 9648, "Mystery" }, { 10749, "Romance" }, { 878, "Science Fiction" },
        { 10770, "TV Movie" }, { 53, "Thriller" }, { 10752, "War" },
        { 37, "Western" }
    };

        var films = new List<Film>();

        foreach (var item in results!)
        {
            var genreIds = item["genre_ids"]?.Select(g => (int)g) ?? new List<int>();
            var genreNames = genreIds.Select(id => genreMap.ContainsKey(id) ? genreMap[id] : "Unknown");
            string genreNamesString = string.Join(", ", genreNames);
            var posterPathValue = item["poster_path"]?.ToString();
            var posterPath = !string.IsNullOrEmpty(posterPathValue)
                ? "https://image.tmdb.org/t/p/w500" + posterPathValue
                : "/images/default-poster.jpg";

            films.Add(new Film
            {
                Title = item.Value<string>("title") ?? "Bilinmiyor",
                PosterUrl = posterPath,
                TMDBId = item.Value<int>("id"),
                ReleaseDate = item.Value<string>("release_date"),
                OriginalLanguage = item.Value<string>("original_language"),
                Genres = genreNamesString
            });
        }

        return new SearchResult
        {
            Films = films,
            TotalPages = totalPages
        };
    }

    public async Task<List<Film>> GetPopularMoviesAsync(int maxPages = 5)
    {
        var allFilms = new List<Film>();

        for (int page = 1; page <= maxPages; page++)
        {
            var url = $"https://api.themoviedb.org/3/movie/popular?api_key={_apiKey}&language=tr-TR&page={page}";
            var response = await _httpClient.GetStringAsync(url);

            var json = JObject.Parse(response);
            var results = json["results"];

            foreach (var item in results!)
            {
                var posterPath = item["poster_path"]?.ToString();
                allFilms.Add(new Film
                {
                    TMDBId = item.Value<int>("id"),
                    Title = item.Value<string>("title") ?? "",
                    PosterUrl = !string.IsNullOrEmpty(posterPath)
                        ? "https://image.tmdb.org/t/p/w500" + posterPath
                        : "/images/default-poster.jpg",
                    Overview = item.Value<string>("overview"),
                    ReleaseDate = item.Value<string>("release_date"),
                    OriginalLanguage = item.Value<string>("original_language")
                });
            }
        }

        return allFilms;
    }

}

public class SearchResult
{
    public List<Film> Films { get; set; } = new();
    public int TotalPages { get; set; }
}
