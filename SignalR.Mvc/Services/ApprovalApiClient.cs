using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SignalR.Mvc.Services;

public sealed record ApiResult<T>(bool Succeeded, int StatusCode, T? Value, string? Error);

public sealed record LoginResult(
    string Token,
    string UserName,
    string DisplayName,
    IReadOnlyList<string> Roles,
    DateTime ExpiresAt);

public sealed record ApprovalItem(
    Guid Id,
    string Title,
    string Details,
    Guid RequesterId,
    string RequesterName,
    string Status,
    string? DecisionNote,
    DateTime CreatedAt,
    DateTime? DecidedAt);

public sealed class ApprovalApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public ApprovalApiClient(HttpClient http)
    {
        _http = http;
    }

    public Task<ApiResult<LoginResult>> LoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
        return SendAsync<LoginResult>(
            () => _http.PostAsJsonAsync("api/auth/login", new { userName, password }, Json, cancellationToken),
            cancellationToken);
    }

    public Task<ApiResult<List<ApprovalItem>>> GetMineAsync(string token, CancellationToken cancellationToken) =>
        GetAsync<List<ApprovalItem>>("api/approvals/mine", token, cancellationToken);

    public Task<ApiResult<List<ApprovalItem>>> GetPendingAsync(string token, CancellationToken cancellationToken) =>
        GetAsync<List<ApprovalItem>>("api/approvals/pending", token, cancellationToken);

    public Task<ApiResult<ApprovalItem>> CreateAsync(string token, string title, string details, CancellationToken cancellationToken)
    {
        return SendAsync<ApprovalItem>(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "api/approvals")
            {
                Content = JsonContent.Create(new { title, details }, options: Json)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return _http.SendAsync(request, cancellationToken);
        }, cancellationToken);
    }

    public Task<ApiResult<ApprovalItem>> DecideAsync(string token, Guid id, bool approved, string? note, CancellationToken cancellationToken)
    {
        return SendAsync<ApprovalItem>(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/approvals/{id}/decide")
            {
                Content = JsonContent.Create(new { approved, note }, options: Json)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return _http.SendAsync(request, cancellationToken);
        }, cancellationToken);
    }

    private async Task<ApiResult<T>> GetAsync<T>(string url, string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _http.SendAsync(request, cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    private async Task<ApiResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        using var response = await send();
        return await ReadAsync<T>(response, cancellationToken);
    }

    private static async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
            return new ApiResult<T>(true, (int)response.StatusCode, value, null);
        }

        return new ApiResult<T>(false, (int)response.StatusCode, default, await ReadErrorAsync(response, cancellationToken));
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return response.ReasonPhrase ?? "The API request failed.";
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? body;
            }

            if (document.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
            return body;
        }

        return body;
    }
}
