namespace Odjezdy;

sealed class WatchEditForm : Form
{
    private readonly TextBox _label;
    private readonly TextBox _from;
    private readonly TextBox _line;
    private readonly TextBox _to;

    public WatchConfig Result { get; }

    public WatchEditForm(WatchConfig watch)
    {
        Result = watch;
        Text = Strings.EditWatch;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;
        Font = SystemFonts.MessageBoxFont;
        Padding = new Padding(16);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        MinimumSize = new Size(420, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _label = Field(layout, Strings.WatchLabel, watch.Label ?? "");
        _from = Field(layout, Strings.WatchFrom, watch.StopId);
        _line = Field(layout, Strings.WatchLine, watch.RouteShortName ?? "");
        _to = Field(layout, Strings.WatchTo, watch.HeadsignContains ?? "");

        var hint = new Label
        {
            Text = Strings.WatchHint,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            Margin = new Padding(0, 4, 0, 16)
        };
        layout.Controls.Add(hint);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 8, 0, 0)
        };
        var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.None, AutoSize = true, Padding = new Padding(12, 3, 12, 3) };
        var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(12, 3, 12, 3) };
        ok.Click += (_, _) => TryAccept();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private static TextBox Field(TableLayoutPanel layout, string caption, string value)
    {
        layout.Controls.Add(new Label { Text = caption, AutoSize = true, Margin = new Padding(0, 8, 0, 2) });
        var box = new TextBox
        {
            Text = value,
            Width = 360,
            Margin = new Padding(0, 0, 0, 4)
        };
        layout.Controls.Add(box);
        return box;
    }

    private void TryAccept()
    {
        if (string.IsNullOrWhiteSpace(_from.Text))
        {
            MessageBox.Show(this, Strings.StopIdRequired, Strings.EditWatch, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Result.Label = _label.Text.Trim();
        Result.StopId = _from.Text.Trim();
        Result.RouteShortName = _line.Text.Trim();
        Result.HeadsignContains = _to.Text.Trim();
        if (string.IsNullOrWhiteSpace(Result.Id))
            Result.Id = Guid.NewGuid().ToString("N")[..10];

        DialogResult = DialogResult.OK;
        Close();
    }
}
