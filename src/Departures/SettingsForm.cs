namespace Departures;

sealed class SettingsForm : Form
{
    private readonly ListBox _list;
    private readonly TextBox _key;
    private readonly CheckBox _startWithWindows;
    private readonly AppConfig _config;
    private readonly Func<Task> _updateTimetable;

    public SettingsForm(AppConfig config, Func<Task> updateTimetable)
    {
        _config = config;
        _updateTimetable = updateTimetable;

        Text = Strings.Settings;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Font;
        Font = SystemFonts.MessageBoxFont;
        Padding = new Padding(16);
        ClientSize = new Size(500, 640);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var watches = new GroupBox { Text = Strings.Watches, Dock = DockStyle.Fill, Padding = new Padding(10) };
        var watchLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        watchLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        watchLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        _list.DoubleClick += (_, _) => EditWatch();
        var watchButtons = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        var add = new Button { Text = Strings.Add, AutoSize = true };
        var edit = new Button { Text = Strings.Edit, AutoSize = true };
        var remove = new Button { Text = Strings.Remove, AutoSize = true };
        add.Click += (_, _) => AddWatch();
        edit.Click += (_, _) => EditWatch();
        remove.Click += (_, _) => RemoveWatch();
        watchButtons.Controls.Add(add);
        watchButtons.Controls.Add(edit);
        watchButtons.Controls.Add(remove);
        watchLayout.Controls.Add(_list, 0, 0);
        watchLayout.Controls.Add(watchButtons, 0, 1);
        watches.Controls.Add(watchLayout);
        root.Controls.Add(watches, 0, 0);

        var golemio = new GroupBox { Text = Strings.GolemioSection, Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10), Margin = new Padding(0, 12, 0, 0) };
        var golemioLayout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1 };
        var optionalRow = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        optionalRow.Controls.Add(new Label
        {
            Text = Strings.GolemioOptional,
            AutoSize = true,
            Margin = new Padding(0, 5, 8, 0)
        });
        var help = new Button
        {
            Text = "?",
            Width = 28,
            Height = 26,
            Margin = new Padding(0),
            AccessibleName = Strings.GolemioHelp
        };
        help.Click += (_, _) => ShowGolemioHelp();
        optionalRow.Controls.Add(help);
        golemioLayout.Controls.Add(optionalRow, 0, 0);

        var keyRow = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Dock = DockStyle.Fill, Margin = new Padding(0) };
        keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        keyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var keyLabel = new Label
        {
            Text = Strings.GolemioKey,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 4, 8, 0)
        };
        _key = new TextBox { Dock = DockStyle.Fill, Text = config.GolemioApiKey ?? "", UseSystemPasswordChar = true };
        keyRow.Controls.Add(keyLabel, 0, 0);
        keyRow.Controls.Add(_key, 1, 0);
        golemioLayout.Controls.Add(keyRow, 0, 1);
        golemio.Controls.Add(golemioLayout);
        root.Controls.Add(golemio, 0, 1);

        var startup = new GroupBox { Text = Strings.AutostartSection, Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10), Margin = new Padding(0, 12, 0, 0) };
        var startupLayout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1 };
        _startWithWindows = new CheckBox
        {
            Text = Strings.StartWithWindows,
            AutoSize = true,
            Checked = Autostart.IsEnabled,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        startupLayout.Controls.Add(_startWithWindows, 0, 0);
        startup.Controls.Add(startupLayout);
        root.Controls.Add(startup, 0, 2);

        var timetable = new GroupBox { Text = Strings.TimetableSection, Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(10), Margin = new Padding(0, 12, 0, 0) };
        var timetableLayout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1 };
        var timetableHint = new Label
        {
            Text = Strings.TimetableHint,
            AutoSize = true,
            MaximumSize = new Size(440, 0),
            Margin = new Padding(0, 0, 0, 8)
        };
        var update = new Button { Text = Strings.UpdateTimetable, AutoSize = true };
        update.Click += async (_, _) =>
        {
            update.Enabled = false;
            try
            {
                await _updateTimetable();
            }
            finally
            {
                update.Enabled = true;
            }
        };
        timetableLayout.Controls.Add(timetableHint, 0, 0);
        timetableLayout.Controls.Add(update, 0, 1);
        timetable.Controls.Add(timetableLayout);
        root.Controls.Add(timetable, 0, 3);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 14, 0, 0)
        };
        var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true, Padding = new Padding(12, 3, 12, 3) };
        var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(12, 3, 12, 3) };
        ok.Click += (_, _) => Apply();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        root.Controls.Add(buttons, 0, 4);

        Controls.Add(root);
        AcceptButton = ok;
        CancelButton = cancel;
        RefreshList();
    }

    private void ShowGolemioHelp()
    {
        using var help = new GolemioHelpForm();
        help.ShowDialog(this);
        _key.Focus();
        _key.SelectAll();
    }

    private void RefreshList()
    {
        _list.Items.Clear();
        foreach (WatchConfig watch in _config.Watches)
            _list.Items.Add(watch.MenuLabel);
    }

    private WatchConfig? SelectedWatch()
    {
        int i = _list.SelectedIndex;
        if (i < 0 || i >= _config.Watches.Count)
            return null;
        return _config.Watches[i];
    }

    private void AddWatch()
    {
        var watch = new WatchConfig();
        using var edit = new WatchEditForm(watch);
        if (edit.ShowDialog(this) != DialogResult.OK)
            return;

        _config.Watches.Add(watch);
        if (string.IsNullOrWhiteSpace(_config.ActiveWatchId))
            _config.ActiveWatchId = watch.Id;
        RefreshList();
        _list.SelectedIndex = _config.Watches.Count - 1;
    }

    private void EditWatch()
    {
        WatchConfig? watch = SelectedWatch();
        if (watch is null)
            return;

        using var edit = new WatchEditForm(watch);
        if (edit.ShowDialog(this) != DialogResult.OK)
            return;

        RefreshList();
    }

    private void RemoveWatch()
    {
        int i = _list.SelectedIndex;
        if (i < 0 || i >= _config.Watches.Count)
            return;

        WatchConfig removed = _config.Watches[i];
        _config.Watches.RemoveAt(i);
        if (string.Equals(_config.ActiveWatchId, removed.Id, StringComparison.OrdinalIgnoreCase))
            _config.ActiveWatchId = _config.Watches.FirstOrDefault()?.Id;
        RefreshList();
    }

    private void Apply()
    {
        _config.GolemioApiKey = _key.Text.Trim();
        if (_config.ActiveWatch is null)
            _config.ActiveWatchId = _config.Watches.FirstOrDefault()?.Id;
        _config.Save();
        ApplyAutostart();
    }

    private void ApplyAutostart()
    {
        try
        {
            if (_startWithWindows.Checked == Autostart.IsEnabled)
                return;

            if (_startWithWindows.Checked)
                Autostart.Enable();
            else
                Autostart.Disable();
        }
        catch
        {
            MessageBox.Show(this, Strings.AutostartFailed, Strings.Settings, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
