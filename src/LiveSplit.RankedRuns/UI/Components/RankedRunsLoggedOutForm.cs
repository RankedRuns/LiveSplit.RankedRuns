using System.Drawing;
using System.Windows.Forms;

namespace LiveSplit.UI.Components;

public sealed class RankedRunsLoggedOutForm : Form
{
    private readonly CheckBox _doNotShowAgainCheckBox;

    public bool DoNotShowAgain => _doNotShowAgainCheckBox.Checked;

    public RankedRunsLoggedOutForm()
    {
        Text = "RankedRuns LiveSplit";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(470, 210);

        var label = new Label
        {
            Left = 16,
            Top = 16,
            Width = 438,
            Height = 95,
            AutoSize = false,
            Text =
                "You are currently not logged in to RankedRuns.com.\r\n" +
                "Runs will not be uploaded.\r\n\r\n" +
                "Please sign in via \"Edit Layout -> RankedRuns.com\"."
        };

        _doNotShowAgainCheckBox = new CheckBox
        {
            Left = 16,
            Top = 120,
            Width = 160,
            Height = 24,
            Text = "Do not show again"
        };

        var okButton = new Button
        {
            Text = "OK",
            Left = 364,
            Top = 160,
            Width = 90,
            Height = 28,
            DialogResult = DialogResult.OK
        };

        Controls.Add(label);
        Controls.Add(_doNotShowAgainCheckBox);
        Controls.Add(okButton);

        AcceptButton = okButton;
    }
}
