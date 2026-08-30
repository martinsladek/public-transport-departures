using System.Diagnostics;

namespace Departures;

sealed class GolemioHelpForm : Form
{
    public GolemioHelpForm()
    {
        Text = Strings.GolemioHelpTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;
        Font = SystemFonts.MessageBoxFont;
        Padding = new Padding(18);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            Dock = DockStyle.Fill
        };

        layout.Controls.Add(new Label
        {
            Text = Strings.GolemioHelpBody,
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Margin = new Padding(0, 0, 0, 12)
        });

        layout.Controls.Add(new Label
        {
            Text = Strings.GolemioHelpSteps,
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Margin = new Padding(0, 0, 0, 12)
        });

        var link = new LinkLabel
        {
            Text = Strings.GolemioKeysUrl,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16)
        };
        link.LinkClicked += (_, _) => OpenUrl(Strings.GolemioKeysUrl);
        layout.Controls.Add(link);

        var ok = new Button
        {
            Text = Strings.Ok,
            DialogResult = DialogResult.OK,
            AutoSize = true,
            Padding = new Padding(12, 3, 12, 3),
            Anchor = AnchorStyles.Right
        };
        layout.Controls.Add(ok);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = ok;
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
        }
    }
}
