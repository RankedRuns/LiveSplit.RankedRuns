using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using System.Xml;

using LiveSplit.Model;

namespace LiveSplit.UI.Components;

public class RunUploaderComponent : LogicComponent
{
    public override string ComponentName => "RankedRuns.com";

    private LiveSplitState State { get; set; }
    private RunUploaderSettings Settings { get; set; }

    private readonly HttpClient _httpClient;

    private const string ProdApiBaseUrl = "https://api.rankedruns.com";
    private const string ProdSiteBaseUrl = "https://rankedruns.com";

    private static string TrimTrailingSlash(string value)
    {
        return value.Trim().TrimEnd('/');
    }

    private string ApiBaseUrl
        => TrimTrailingSlash(
            Environment.GetEnvironmentVariable("RANKEDRUNS_API_BASE_URL")
            ?? ProdApiBaseUrl
        );

    private string SiteBaseUrl
        => TrimTrailingSlash(
            Environment.GetEnvironmentVariable("RANKEDRUNS_SITE_BASE_URL")
            ?? ProdSiteBaseUrl
        );

    private string SubmitRunUrl => ApiBaseUrl + "/integrations/livesplit/runs";
    private string PresenceUrl => ApiBaseUrl + "/integrations/livesplit/presence";
    private string LiveSplitCodeUrl => ApiBaseUrl + "/integrations/livesplit/auth/code";
    private string LiveSplitPollUrl => ApiBaseUrl + "/integrations/livesplit/auth/poll";
    private string LiveSplitRefreshUrl => ApiBaseUrl + "/integrations/livesplit/auth/refresh";
    private string LiveSplitMeUrl => ApiBaseUrl + "/integrations/livesplit/auth/me";

    private enum PluginAuthState
    {
        NotLinked,
        Checking,
        Linked,
        Expired,
        ServiceUnavailable
    }

    private PluginAuthState _authState = PluginAuthState.NotLinked;

    private readonly LiveSplitAuthClient _auth;

    private string _gameName = "";
    private string _categoryName = "";

    private DateTime _lastPresenceSentUtc = DateTime.MinValue;
    private bool _presenceInFlight = false;
    private bool _runSubmitInFlight = false;

    private string _linkedUsername = null;
    private bool _logoutNoticeShownThisSession = false;

    private sealed class MeResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; }
    }

    public RunUploaderComponent(LiveSplitState state)
    {
        State = state;
        Settings = new RunUploaderSettings();

        Settings.AuthButtonClicked += async (_, __) =>
        {
            await HandleAuthButtonClicked();
        };

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(8)
        };

        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");

        _auth = new LiveSplitAuthClient(
            _httpClient,
            getRefreshToken: () => Settings.GetRefreshToken(),
            setRefreshToken: rt => Settings.SetRefreshToken(rt),
            clearRefreshToken: () => Settings.ClearRefreshToken()
        );

        SetPluginAuthState(PluginAuthState.NotLinked); //bootstrap from setSettings

        SetGameAndCategory();

        State.OnStart += HandleStateChange;
        State.OnSplit += HandleStateChange;
        State.OnSkipSplit += HandleStateChange;
        State.OnUndoSplit += HandleStateChange;
        State.OnUndoAllPauses += HandleStateChange;
        State.OnPause += HandleStateChange;
        State.OnResume += HandleStateChange;
        State.OnReset += HandleStateChange;

        State.OnSplit += HandleFinished;
    }

    private void SetPluginAuthState(PluginAuthState state, string username = null)
    {
        _authState = state;

        switch (state)
        {
            case PluginAuthState.NotLinked:
                Settings.SetAuthUi(RunUploaderSettings.AuthUiState.NotLinked, null);
                break;

            case PluginAuthState.Checking:
                Settings.SetAuthUi(RunUploaderSettings.AuthUiState.Checking, username);
                break;

            case PluginAuthState.Linked:
                Settings.SetAuthUi(RunUploaderSettings.AuthUiState.Linked, username);
                break;

            case PluginAuthState.Expired:
                Settings.SetAuthUi(RunUploaderSettings.AuthUiState.Expired, null);
                break;

            case PluginAuthState.ServiceUnavailable:
                Settings.SetAuthUi(RunUploaderSettings.AuthUiState.ServiceUnavailable, username);
                break;
        }
    }

    private void SetGameAndCategory()
    {
        _gameName = State.Run.GameName;
        _categoryName = State.Run.CategoryName;
    }

    private bool AreSplitsValid()
    {
        return _gameName != "" && _categoryName != "";
    }

    private void LogException(string prefix, Exception ex)
    {
        try
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RankedRunsLiveSplit.log"),
                DateTime.UtcNow + " " + prefix + " " + ex + Environment.NewLine);
        }
        catch
        {
        }
    }

    private void MaybeShowLoggedOutNotice()
    {
        if (!Settings.ShowLoggedOutNotice || _logoutNoticeShownThisSession)
        {
            return;
        }

        _logoutNoticeShownThisSession = true;

        if (Settings.InvokeRequired)
        {
            Settings.BeginInvoke(new Action(ShowLoggedOutNoticeDialog));
            return;
        }

        ShowLoggedOutNoticeDialog();
    }

    private void ShowLoggedOutNoticeDialog()
    {
        using var form = new RankedRunsLoggedOutForm();
        DialogResult result = form.ShowDialog();

        if (result == DialogResult.OK && form.DoNotShowAgain)
        {
            Settings.ShowLoggedOutNotice = false;
        }
    }

    private int? GetFinalTimeMs()
    {
        TimingMethod method = State.CurrentTimingMethod;

        TimeSpan? cur = State.CurrentTime[method];
        if (cur != null)
        {
            return (int)Math.Round(((TimeSpan)cur).TotalMilliseconds);
        }

        ISegment last = null;
        foreach (ISegment seg in State.Run)
        {
            last = seg;
        }

        if (last != null)
        {
            TimeSpan? t = last.SplitTime[method];
            if (t != null)
            {
                return (int)Math.Round(((TimeSpan)t).TotalMilliseconds);
            }
        }

        return null;
    }

    private int? GetPersonalBestTimeMs()
    {
        TimingMethod method = State.CurrentTimingMethod;

        ISegment last = null;
        foreach (ISegment seg in State.Run)
        {
            last = seg;
        }

        if (last == null)
        {
            return null;
        }

        TimeSpan? pb = last.PersonalBestSplitTime[method];
        if (pb == null)
        {
            return null;
        }

        return (int)Math.Round(((TimeSpan)pb).TotalMilliseconds);
    }

    private ISegment GetSegmentAtIndex(int idx)
    {
        if (idx < 0)
        {
            return null;
        }

        int i = 0;
        foreach (ISegment seg in State.Run)
        {
            if (i == idx)
            {
                return seg;
            }

            i++;
        }

        return null;
    }

    private double? ComputeDeltaToCurrentComparisonMs()
    {
        TimingMethod method = State.CurrentTimingMethod;
        string comparison = State.CurrentComparison;

        int idx = State.CurrentSplitIndex - 1;
        if (idx < 0)
        {
            return null;
        }

        ISegment seg = GetSegmentAtIndex(idx);
        if (seg == null)
        {
            return null;
        }

        TimeSpan? actual = seg.SplitTime[method];
        if (actual == null)
        {
            return null;
        }

        if (!seg.Comparisons.TryGetValue(comparison, out Time compTime))
        {
            return null;
        }

        TimeSpan? comp = compTime[method];
        if (comp == null)
        {
            return null;
        }

        return (((TimeSpan)actual) - ((TimeSpan)comp)).TotalMilliseconds;
    }

    private object BuildPresencePayload()
    {
        IRun run = State.Run;
        TimingMethod method = State.CurrentTimingMethod;

        double? currentTimeMs = null;
        TimeSpan? cur = State.CurrentTime[method];
        if (cur != null)
        {
            currentTimeMs = ((TimeSpan)cur).TotalMilliseconds;
        }

        ISegment currentSeg = State.CurrentSplit;
        string splitName = currentSeg != null ? currentSeg.Name : "";
        int splitIndex = State.CurrentSplitIndex;
        string phase = State.CurrentPhase.ToString();
        string currentComparison = State.CurrentComparison;

        double? deltaToComparisonMs = ComputeDeltaToCurrentComparisonMs();

        return new
        {
            gameName = _gameName,
            categoryName = _categoryName,
            platformName = run.Metadata.PlatformName,
            regionText = run.Metadata.RegionName,
            isEmulator = run.Metadata.UsesEmulator,

            timingMethod = method.ToString(),
            currentTimeMs = currentTimeMs,
            splitIndex = splitIndex,
            splitName = splitName,
            deltaToComparisonMs = deltaToComparisonMs,
            phase = phase,
            currentComparison = currentComparison,

            updatedAt = DateTime.UtcNow,

            installId = Settings.InstallId,
            clientVersion = Settings.ClientVersion,
        };
    }

    private object BuildSplitsSnapshot()
    {
        TimingMethod method = State.CurrentTimingMethod;
        string comparison = State.CurrentComparison;

        var list = new System.Collections.Generic.List<object>();

        int idx = 0;
        TimeSpan? prevSplit = null;

        foreach (ISegment seg in State.Run)
        {
            TimeSpan? split = seg.SplitTime[method];
            if (split == null)
            {
                idx++;
                continue;
            }

            var splitTs = (TimeSpan)split;
            TimeSpan segmentTs = prevSplit != null ? (splitTs - prevSplit.Value) : splitTs;

            int splitMs = (int)Math.Round(splitTs.TotalMilliseconds);
            int segmentMs = (int)Math.Round(segmentTs.TotalMilliseconds);

            bool gold = false;
            TimeSpan? best = seg.BestSegmentTime[method];
            if (best != null)
            {
                var bestTs = (TimeSpan)best;
                gold = segmentTs.Ticks <= bestTs.Ticks;
            }

            int? deltaMs = null;
            if (seg.Comparisons.TryGetValue(comparison, out var compTime))
            {
                TimeSpan? comp = compTime[method];
                if (comp != null)
                {
                    TimeSpan deltaTs = splitTs - (TimeSpan)comp;
                    deltaMs = (int)Math.Round(deltaTs.TotalMilliseconds);
                }
            }

            list.Add(new
            {
                index = idx,
                name = seg.Name ?? "",
                splitTimeMs = splitMs,
                segmentTimeMs = segmentMs,
                deltaToComparisonMs = deltaMs,
                gold = gold
            });

            prevSplit = splitTs;
            idx++;
        }

        return list;
    }

    private bool ShouldSendPresence()
    {
        DateTime now = DateTime.UtcNow;

        if (_presenceInFlight)
        {
            return false;
        }

        if ((now - _lastPresenceSentUtc).TotalSeconds < 5)
        {
            return false;
        }

        return true;
    }

    public async Task SendPresence()
    {
        object payload = BuildPresencePayload();
        string json = new JavaScriptSerializer().Serialize(payload);

        HttpResponseMessage resp = await _auth.SendAuthedAsync(
            buildRequest: () =>
            {
                var req = new HttpRequestMessage(HttpMethod.Post, PresenceUrl)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
                return req;
            },
            refreshUrl: LiveSplitRefreshUrl,
            ct: CancellationToken.None
        );

        if (resp.StatusCode == HttpStatusCode.Unauthorized)
        {
            resp.Dispose();
            _linkedUsername = null;
            SetPluginAuthState(PluginAuthState.Expired);
            return;
        }

        if (!resp.IsSuccessStatusCode)
        {
            SetPluginAuthState(PluginAuthState.ServiceUnavailable, _linkedUsername);
        }

        resp.Dispose();
    }

    private async Task SubmitFinishedRun()
    {
        SetGameAndCategory();

        if (!_auth.HasAccessToken && !Settings.HasRefreshToken())
        {
            return;
        }

        if (!AreSplitsValid() || _runSubmitInFlight)
        {
            return;
        }

        int? timeMs = GetFinalTimeMs();
        if (timeMs == null)
        {
            return;
        }

        if (timeMs.Value < 30000)
        {
            return;
        }

        int? personalBestTimeMs = GetPersonalBestTimeMs();

        var payload = new
        {
            gameName = _gameName,
            categoryName = _categoryName,
            timeMs = timeMs.Value,
            personalBestTimeMs = personalBestTimeMs,
            timingMethod = State.CurrentTimingMethod.ToString(),
            createdAt = DateTime.UtcNow.ToString("O"),

            platformName = State.Run.Metadata.PlatformName,
            regionText = State.Run.Metadata.RegionName,
            isEmulator = State.Run.Metadata.UsesEmulator,

            comparisonName = State.CurrentComparison,
            splits = BuildSplitsSnapshot(),

            installId = Settings.InstallId,
            clientVersion = Settings.ClientVersion,
        };

        try
        {
            _runSubmitInFlight = true;

            string json = new JavaScriptSerializer().Serialize(payload);

            HttpResponseMessage resp = await _auth.SendAuthedAsync(
                buildRequest: () =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, SubmitRunUrl)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                    return req;
                },
                refreshUrl: LiveSplitRefreshUrl,
                ct: CancellationToken.None
            );

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                resp.Dispose();
                _linkedUsername = null;
                SetPluginAuthState(PluginAuthState.Expired);
                return;
            }

            if (!resp.IsSuccessStatusCode)
            {
                SetPluginAuthState(PluginAuthState.ServiceUnavailable, _linkedUsername);
            }

            resp.Dispose();
        }
        finally
        {
            _runSubmitInFlight = false;
        }
    }

    private async Task HandleAuthButtonClicked()
    {
        switch (_authState)
        {
            case PluginAuthState.Linked:
                _auth.ClearAllTokens();
                _linkedUsername = null;
                SetPluginAuthState(PluginAuthState.NotLinked);

                MessageBox.Show(
                    "Successfully logged out.",
                    "RankedRuns LiveSplit",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;

            case PluginAuthState.Checking:
                return;

            case PluginAuthState.ServiceUnavailable:
                if (Settings.HasRefreshToken())
                {
                    SetPluginAuthState(PluginAuthState.Checking, _linkedUsername);
                    await TryUpdateUsernameLabel();
                    return;
                }

                break;

            case PluginAuthState.Expired:
            case PluginAuthState.NotLinked:
            default:
                break;
        }

        await HandleSignInClicked();
    }

    private async Task HandleSignInClicked()
    {
        using var cts = new CancellationTokenSource();

        try
        {
            string clientName = Environment.MachineName;
            string code = await _auth.CreateLinkCodeAsync(
                LiveSplitCodeUrl,
                clientName,
                cts.Token);

            _auth.OpenBrowserToLinkPage(SiteBaseUrl, code);

            bool ok = await _auth.PollUntilLinkedAsync(
                pollUrl: LiveSplitPollUrl,
                code: code,
                pollInterval: TimeSpan.FromSeconds(2),
                timeout: TimeSpan.FromMinutes(10),
                ct: cts.Token
            );

            if (!ok)
            {
                MessageBox.Show(
                    "Linking failed or timed out. Please try again.",
                    "RankedRuns LiveSplit",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _logoutNoticeShownThisSession = false;
            SetPluginAuthState(PluginAuthState.Linked, null);
            await TryUpdateUsernameLabel();

            MessageBox.Show(
                "LiveSplit linked successfully! You can close the browser tab now.",
                "RankedRuns LiveSplit",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show(
                "Sign in was cancelled.",
                "RankedRuns LiveSplit",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            LogException("SIGNIN", ex);

            MessageBox.Show(
                "Sign in failed. Check log file for details.",
                "RankedRuns LiveSplit",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task TryUpdateUsernameLabel()
    {
        try
        {
            HttpResponseMessage resp = await _auth.SendAuthedAsync(
                buildRequest: () => new HttpRequestMessage(HttpMethod.Get, LiveSplitMeUrl),
                refreshUrl: LiveSplitRefreshUrl,
                ct: CancellationToken.None
            );

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                resp.Dispose();
                _linkedUsername = null;
                SetPluginAuthState(PluginAuthState.Expired);
                MaybeShowLoggedOutNotice();
                return;
            }

            if (!resp.IsSuccessStatusCode)
            {
                resp.Dispose();
                SetPluginAuthState(PluginAuthState.ServiceUnavailable, _linkedUsername);
                return;
            }

            string body = await resp.Content.ReadAsStringAsync();
            resp.Dispose();

            MeResponse dto = new JavaScriptSerializer().Deserialize<MeResponse>(body);
            if (dto != null && !string.IsNullOrEmpty(dto.Username))
            {
                _linkedUsername = dto.Username;
                SetPluginAuthState(PluginAuthState.Linked, _linkedUsername);
                return;
            }

            SetPluginAuthState(PluginAuthState.ServiceUnavailable, _linkedUsername);
        }
        catch (Exception ex)
        {
            LogException("ME", ex);
            SetPluginAuthState(PluginAuthState.ServiceUnavailable, _linkedUsername);
        }
    }

    private async void HandleFinished(object sender, object e)
    {
        try
        {
            if (State.CurrentSplitIndex == State.Run.Count)
            {
                await SubmitFinishedRun();
            }
        }
        catch (Exception ex)
        {
            LogException("FINISHED", ex);
        }
    }

    private async void HandleStateChange(object sender, object e)
    {
        await HandleStateChangeInternal();
    }

    private async void HandleStateChange(object sender, TimerPhase phase)
    {
        await HandleStateChangeInternal();
    }

    private async Task HandleStateChangeInternal()
    {
        try
        {
            SetGameAndCategory();

            if (!_auth.HasAccessToken && !Settings.HasRefreshToken())
            {
                return;
            }

            if (!AreSplitsValid() || !Settings.IsRichPresenceEnabled)
            {
                return;
            }

            if (!ShouldSendPresence())
            {
                return;
            }

            _presenceInFlight = true;
            await SendPresence();
            _lastPresenceSentUtc = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            LogException("PRESENCE", ex);
        }
        finally
        {
            _presenceInFlight = false;
        }
    }

    private void RestoreAuthStateFromSettings(bool allowPopup)
    {
        if (Settings.HasRefreshToken())
        {
            SetPluginAuthState(PluginAuthState.Checking, _linkedUsername);
            _ = TryUpdateUsernameLabel();
        }
        else
        {
            SetPluginAuthState(PluginAuthState.NotLinked);

            if (allowPopup)
            {
                MaybeShowLoggedOutNotice();
            }
        }
    }

    public override void Dispose()
    {
        State.OnStart -= HandleStateChange;
        State.OnSplit -= HandleStateChange;
        State.OnSkipSplit -= HandleStateChange;
        State.OnUndoSplit -= HandleStateChange;
        State.OnUndoAllPauses -= HandleStateChange;
        State.OnPause -= HandleStateChange;
        State.OnResume -= HandleStateChange;
        State.OnReset -= HandleStateChange;

        State.OnSplit -= HandleFinished;

        _httpClient.Dispose();
    }

    public override XmlNode GetSettings(XmlDocument document)
    {
        return Settings.GetSettings(document);
    }

    public override Control GetSettingsControl(LayoutMode mode)
    {
        Settings.Mode = mode;

        if (Settings.HasRefreshToken())
        {
            SetPluginAuthState(PluginAuthState.Checking, _linkedUsername);
            _ = TryUpdateUsernameLabel();
        }
        else
        {
            SetPluginAuthState(PluginAuthState.NotLinked);
        }

        return Settings;
    }

    public override void SetSettings(XmlNode settings)
    {
        Settings.SetSettings(settings);
        RestoreAuthStateFromSettings(allowPopup: true);
    }

    public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
    {
    }
}
