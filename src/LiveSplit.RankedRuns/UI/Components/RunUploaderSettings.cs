using System;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components;

public partial class RunUploaderSettings : UserControl
{
    public enum AuthUiState
    {
        NotLinked,
        Checking,
        Linked,
        Expired,
        ServiceUnavailable
    }

    public LayoutMode Mode { get; set; }

    public string InstallId { get; private set; } = "";
    public string ClientVersion { get; private set; } = "";

    public string Path { get; set; }
    public bool IsRichPresenceEnabled { get; set; }
    public bool ShowLoggedOutNotice { get; set; }

    public event EventHandler AuthButtonClicked;

    private string _linkedUsername;

    public RunUploaderSettings()
    {
        InitializeComponent();

        ClientVersion = typeof(RunUploaderSettings).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        if (string.IsNullOrWhiteSpace(InstallId))
        {
            InstallId = Guid.NewGuid().ToString();
        }

        chkRichPresenceEnabled.DataBindings.Add(
            "Checked",
            this,
            "IsRichPresenceEnabled",
            false,
            DataSourceUpdateMode.OnPropertyChanged);

        chkShowLoggedOutNotice.DataBindings.Add(
            "Checked",
            this,
            "ShowLoggedOutNotice",
            false,
            DataSourceUpdateMode.OnPropertyChanged);

        Path = "";
        IsRichPresenceEnabled = true;
        ShowLoggedOutNotice = true;

        SetAuthUi(AuthUiState.NotLinked, null);
    }

    public void SetSettings(XmlNode node)
    {
        var element = (XmlElement)node;

        Version version = SettingsHelper.ParseVersion(element["Version"]);
        Path = SettingsHelper.ParseString(element["Path"]);

        IsRichPresenceEnabled =
            element["IsRichPresenceEnabled"] == null ||
            SettingsHelper.ParseBool(element["IsRichPresenceEnabled"]);

        ShowLoggedOutNotice =
            element["ShowLoggedOutNotice"] == null ||
            SettingsHelper.ParseBool(element["ShowLoggedOutNotice"]);

        InstallId = element["InstallId"] != null
            ? SettingsHelper.ParseString(element["InstallId"])
            : InstallId;

        if (string.IsNullOrWhiteSpace(InstallId))
        {
            InstallId = Guid.NewGuid().ToString();
        }

        ClientVersion = typeof(RunUploaderSettings).Assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    public XmlNode GetSettings(XmlDocument document)
    {
        XmlElement parent = document.CreateElement("Settings");
        CreateSettingsNode(document, parent);
        return parent;
    }

    public int GetSettingsHashCode()
    {
        return CreateSettingsNode(null, null);
    }

    private int CreateSettingsNode(XmlDocument document, XmlElement parent)
    {
        return SettingsHelper.CreateSetting(document, parent, "Version", "1.0.0") ^
            SettingsHelper.CreateSetting(document, parent, "InstallId", InstallId) ^
            SettingsHelper.CreateSetting(document, parent, "ClientVersion", ClientVersion) ^
            SettingsHelper.CreateSetting(document, parent, "IsRichPresenceEnabled", IsRichPresenceEnabled) ^
            SettingsHelper.CreateSetting(document, parent, "ShowLoggedOutNotice", ShowLoggedOutNotice);
    }

    public void SetAuthUi(AuthUiState state, string usernameOrNull)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetAuthUi(state, usernameOrNull)));
            return;
        }

        _linkedUsername = usernameOrNull;

        switch (state)
        {
            case AuthUiState.NotLinked:
                AuthButton.Text = "Sign in";
                AuthButton.Enabled = true;
                lnkAuthStatus.Text = "Not linked";
                lnkAuthStatus.LinkBehavior = LinkBehavior.NeverUnderline;
                lnkAuthStatus.TabStop = false;
                lnkAuthStatus.Enabled = false;
                break;

            case AuthUiState.Checking:
                AuthButton.Text = "Checking...";
                AuthButton.Enabled = false;
                lnkAuthStatus.Text = "Checking saved session...";
                lnkAuthStatus.LinkBehavior = LinkBehavior.NeverUnderline;
                lnkAuthStatus.TabStop = false;
                lnkAuthStatus.Enabled = false;
                break;

            case AuthUiState.Linked:
                AuthButton.Text = "Sign out";
                AuthButton.Enabled = true;

                if (string.IsNullOrEmpty(usernameOrNull))
                {
                    lnkAuthStatus.Text = "Linked";
                    lnkAuthStatus.LinkBehavior = LinkBehavior.NeverUnderline;
                    lnkAuthStatus.TabStop = false;
                    lnkAuthStatus.Enabled = false;
                }
                else
                {
                    lnkAuthStatus.Text = "Linked as: " + usernameOrNull;
                    lnkAuthStatus.LinkBehavior = LinkBehavior.HoverUnderline;
                    lnkAuthStatus.TabStop = true;
                    lnkAuthStatus.Enabled = true;
                }

                break;

            case AuthUiState.Expired:
                AuthButton.Text = "Sign in";
                AuthButton.Enabled = true;
                lnkAuthStatus.Text = "Saved session expired. Please sign in again.";
                lnkAuthStatus.LinkBehavior = LinkBehavior.NeverUnderline;
                lnkAuthStatus.TabStop = false;
                lnkAuthStatus.Enabled = false;
                break;

            case AuthUiState.ServiceUnavailable:
                AuthButton.Text = "Retry";
                AuthButton.Enabled = true;
                lnkAuthStatus.Text = string.IsNullOrEmpty(usernameOrNull)
                    ? "Service unavailable. Saved link could not be checked."
                    : "Service unavailable. Last linked user: " + usernameOrNull;

                lnkAuthStatus.LinkBehavior = string.IsNullOrEmpty(usernameOrNull)
                    ? LinkBehavior.NeverUnderline
                    : LinkBehavior.HoverUnderline;

                lnkAuthStatus.TabStop = !string.IsNullOrEmpty(usernameOrNull);
                lnkAuthStatus.Enabled = !string.IsNullOrEmpty(usernameOrNull);
                break;
        }
    }

    public string GetRefreshToken()
    {
        try
        {
            string protectedToken =
                RunUploaderAuthStorage.ReadRefreshTokenProtected();

            return LiveSplitAuthClient.Unprotect(protectedToken);
        }
        catch
        {
            return "";
        }
    }

    public void SetRefreshToken(string refreshToken)
    {
        string protectedToken =
            LiveSplitAuthClient.Protect(refreshToken ?? "");

        RunUploaderAuthStorage.WriteRefreshTokenProtected(protectedToken);
    }

    public void ClearRefreshToken()
    {
        RunUploaderAuthStorage.Clear();
    }

    public bool HasRefreshToken()
    {
        return !string.IsNullOrEmpty(GetRefreshToken());
    }

    private void AuthButtonClick(object sender, EventArgs e)
    {
        AuthButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    private void TableLayoutPanel1_Paint(object sender, PaintEventArgs e)
    {
    }

    private void ChkRichPresenceEnabled_CheckedChanged(object sender, EventArgs e)
    {
    }

    private void RankedRunsLabel_Click_1(object sender, EventArgs e)
    {
    }

    private void ChkShowLoggedOutNotice_CheckedChanged(object sender, EventArgs e)
    {
    }

    private void RankedRunsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        OpenUrl("https://www.rankedruns.com");
    }

    private void LnkAuthStatus_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_linkedUsername))
        {
            return;
        }

        OpenUrl("https://rankedruns.com/users/" + Uri.EscapeDataString(_linkedUsername));
    }

    private static void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }
}
