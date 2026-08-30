using System.Net.Http.Json;
using MMORPG2D.Shared.Models;

namespace MMORPG2D.Client.Net;

/// <summary>
/// API 서버를 부르는 얇은 래퍼. X-Token 헤더를 자동 부착한다.
/// 2주차에서는 콘솔 데모로, 3주차 이후에는 MonoGame 씬에서 호출한다.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    public string? Token { get; private set; }
    public long AccountId { get; private set; }

    public ApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public async Task<bool> RegisterAsync(string id, string pw)
    {
        var resp = await _http.PostAsJsonAsync("/api/register", new { id, pw });
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> LoginAsync(string id, string pw)
    {
        var resp = await _http.PostAsJsonAsync("/api/login", new { id, pw });
        if (!resp.IsSuccessStatusCode) return false;
        var dto = await resp.Content.ReadFromJsonAsync<LoginResp>();
        if (dto == null) return false;
        Token = dto.token;
        AccountId = dto.accountId;
        return true;
    }

    public async Task<List<WorldDto>> GetWorldsAsync()
    {
        var req = MakeReq(HttpMethod.Get, "/api/worlds");
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return new();
        return await resp.Content.ReadFromJsonAsync<List<WorldDto>>() ?? new();
    }

    public async Task<List<CharacterDto>> ListCharactersAsync()
    {
        var req = MakeReq(HttpMethod.Get, "/api/character/list");
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return new();
        return await resp.Content.ReadFromJsonAsync<List<CharacterDto>>() ?? new();
    }

    public async Task<long> CreateCharacterAsync(int worldId, string name)
    {
        var req = MakeReq(HttpMethod.Post, "/api/character/create");
        req.Content = JsonContent.Create(new { worldId, name });
        var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return 0;
        var dto = await resp.Content.ReadFromJsonAsync<CharacterCreateResp>();
        return dto?.id ?? 0;
    }

    private HttpRequestMessage MakeReq(HttpMethod method, string path)
    {
        var req = new HttpRequestMessage(method, path);
        if (!string.IsNullOrEmpty(Token)) req.Headers.Add("X-Token", Token);
        return req;
    }

    private record LoginResp(string token, long accountId);
    private record CharacterCreateResp(bool ok, long id, string? reason);
}
