using System.Net.Http.Json;

namespace CoachPadWpf;

public sealed class OutcomeClient
{
    private readonly HttpClient _client = new();

    public async Task SendOutcomeAsync(string host, string outcome)
    {
        var response = await _client.PostAsJsonAsync($"http://{host}/outcome", new { outcome });
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> FetchSummaryAsync(string host)
    {
        var response = await _client.GetAsync($"http://{host}/summary");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        if (payload is null || !payload.TryGetValue("summary", out var summary))
        {
            return string.Empty;
        }
        return summary;
    }
}
