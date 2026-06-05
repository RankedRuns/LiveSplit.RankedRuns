using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace LiveSplit.UI.Components;

public sealed class LiveSplitAuthClient
{
    private readonly HttpClient _http;
    private readonly Func<string> _getRefreshToken;
    private readonly Action<string> _setRefreshToken;
    private readonly Action _clearRefreshToken;

    private string _accessToken;

    public enum RefreshResult
    {
        Success,
        InvalidToken,
        ServiceUnavailable
    }

    public RefreshResult LastRefreshResult { get; private set; }

    public LiveSplitAuthClient(
        HttpClient http,
        Func<string> getRefreshToken,
        Action<string> setRefreshToken,
        Action clearRefreshToken)
    {
        _http = http;
        _getRefreshToken = getRefreshToken;
        _setRefreshToken = setRefreshToken;
        _clearRefreshToken = clearRefreshToken;
    }

    public bool HasRefreshToken => !string.IsNullOrEmpty(_getRefreshToken());
    public bool HasAccessToken => !string.IsNullOrEmpty(_accessToken);

    public void SetAccessToken(string token)
    {
        _accessToken = token;
    }

    public void ClearAllTokens()
    {
        _accessToken = null;
        _clearRefreshToken();
    }

    private sealed class CodeResponse
    {
        public string Code { get; set; }
        public string ExpiresAt { get; set; }
    }

    private sealed class PollPending
    {
        public string Status { get; set; }
    }

    private sealed class PollLinked
    {
        public string Status { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    private sealed class RefreshResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    public async Task<string> CreateLinkCodeAsync(string codeUrl, string clientName, CancellationToken ct = default)
    {
        var payload = new { clientName = clientName ?? "" };
        string json = new JavaScriptSerializer().Serialize(payload);

        using HttpResponseMessage resp = await _http.PostAsync(
            codeUrl,
            new StringContent(json, Encoding.UTF8, "application/json"),
            ct);

        resp.EnsureSuccessStatusCode();

        string body = await resp.Content.ReadAsStringAsync();
        CodeResponse dto = new JavaScriptSerializer().Deserialize<CodeResponse>(body);

        if (dto == null || string.IsNullOrEmpty(dto.Code))
        {
            throw new InvalidOperationException("Link code response did not contain a code.");
        }

        return dto.Code;
    }

    public void OpenBrowserToLinkPage(string siteBaseUrl, string code)
    {
        string url = siteBaseUrl.TrimEnd('/') + "/link?code=" + Uri.EscapeDataString(code);
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    public async Task<bool> PollUntilLinkedAsync(
        string pollUrl,
        string code,
        TimeSpan pollInterval,
        TimeSpan timeout,
        CancellationToken ct)
    {
        DateTime started = DateTime.UtcNow;
        var ser = new JavaScriptSerializer();

        while (DateTime.UtcNow - started < timeout)
        {
            ct.ThrowIfCancellationRequested();

            var payload = new { code = code };
            string json = ser.Serialize(payload);

            using HttpResponseMessage resp = await _http.PostAsync(
                pollUrl,
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            resp.EnsureSuccessStatusCode();

            string body = await resp.Content.ReadAsStringAsync();

            PollPending pending = ser.Deserialize<PollPending>(body);
            if (pending != null && pending.Status == "PENDING")
            {
                await Task.Delay(pollInterval, ct);
                continue;
            }

            if (pending != null &&
                (pending.Status == "EXPIRED" ||
                 pending.Status == "NOT_FOUND" ||
                 pending.Status == "CONSUMED"))
            {
                return false;
            }

            PollLinked linked = ser.Deserialize<PollLinked>(body);
            if (linked != null &&
                linked.Status == "LINKED" &&
                !string.IsNullOrEmpty(linked.AccessToken) &&
                !string.IsNullOrEmpty(linked.RefreshToken))
            {
                _accessToken = linked.AccessToken;
                _setRefreshToken(linked.RefreshToken);
                return true;
            }

            await Task.Delay(pollInterval, ct);
        }

        return false;
    }

    public async Task<HttpResponseMessage> SendAuthedAsync(
        Func<HttpRequestMessage> buildRequest,
        string refreshUrl,
        CancellationToken ct)
    {
        HttpResponseMessage resp = await SendWithAccessAsync(buildRequest, ct);

        if (resp.StatusCode != HttpStatusCode.Unauthorized)
        {
            return resp;
        }

        resp.Dispose();

        RefreshResult refreshed = await TryRefreshAsync(refreshUrl, ct);

        if (refreshed == RefreshResult.InvalidToken)
        {
            ClearAllTokens();
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        if (refreshed == RefreshResult.ServiceUnavailable)
        {
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }

        return await SendWithAccessAsync(buildRequest, ct);
    }

    private async Task<HttpResponseMessage> SendWithAccessAsync(
        Func<HttpRequestMessage> buildRequest,
        CancellationToken ct)
    {
        HttpRequestMessage req = buildRequest();

        try
        {
            if (!string.IsNullOrEmpty(_accessToken))
            {
                req.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
            }

            return await _http.SendAsync(req, ct);
        }
        catch
        {
            req.Dispose();
            throw;
        }
    }

    private async Task<RefreshResult> TryRefreshAsync(string refreshUrl, CancellationToken ct)
    {
        string refreshToken = _getRefreshToken();

        if (string.IsNullOrEmpty(refreshToken))
        {
            LastRefreshResult = RefreshResult.InvalidToken;
            return RefreshResult.InvalidToken;
        }

        try
        {
            var payload = new { refreshToken = refreshToken };
            string json = new JavaScriptSerializer().Serialize(payload);

            using HttpResponseMessage resp = await _http.PostAsync(
                refreshUrl,
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            if (resp.StatusCode == HttpStatusCode.Unauthorized ||
                resp.StatusCode == HttpStatusCode.Forbidden)
            {
                LastRefreshResult = RefreshResult.InvalidToken;
                return RefreshResult.InvalidToken;
            }

            if (resp.StatusCode != HttpStatusCode.OK)
            {
                LastRefreshResult = RefreshResult.ServiceUnavailable;
                return RefreshResult.ServiceUnavailable;
            }

            string body = await resp.Content.ReadAsStringAsync();
            RefreshResponse dto = new JavaScriptSerializer().Deserialize<RefreshResponse>(body);

            if (dto == null ||
                string.IsNullOrEmpty(dto.AccessToken) ||
                string.IsNullOrEmpty(dto.RefreshToken))
            {
                LastRefreshResult = RefreshResult.ServiceUnavailable;
                return RefreshResult.ServiceUnavailable;
            }

            _accessToken = dto.AccessToken;
            _setRefreshToken(dto.RefreshToken);

            LastRefreshResult = RefreshResult.Success;
            return RefreshResult.Success;
        }
        catch (Exception ex) when (
            ex is HttpRequestException or
            TaskCanceledException or
            WebException)
        {
            LastRefreshResult = RefreshResult.ServiceUnavailable;
            return RefreshResult.ServiceUnavailable;
        }
    }

    public static string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return "";
        }

        byte[] bytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string protectedB64)
    {
        if (string.IsNullOrEmpty(protectedB64))
        {
            return "";
        }

        byte[] protectedBytes = Convert.FromBase64String(protectedB64);
        byte[] bytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(bytes);
    }
}
