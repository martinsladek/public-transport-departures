namespace Odjezdy;

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
        ClientSize = new Size(460, 500);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 9
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(new Label { Text = Strings.Watches, AutoSize = true, Margin = new Padding(0, 0, 0, 4) }, 0, 0);

        _list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        layout.Controls.Add(_list, 0, 1);

        var watchButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 12)
        };
        var add = new Button { Text = Strings.Add, AutoSize = true };
        var edit = new Button { Text = Strings.Edit, AutoSize = true };
        var remove = new Button { Text = Strings.Remove, AutoSize = true };
        add.Click += (_, _) => AddWatch();
        edit.Click += (_, _) => EditWatch();
        remove.Click += (_, _) => RemoveWatch();
        _list.DoubleClick += (_, _) => EditWatch();
        watchButtons.Controls.Add(add);
        watchButtons.Controls.Add(edit);
        watchButtons.Controls.Add(remove);
        layout.Controls.Add(watchButtons, 0, 2);

        layout.Controls.Add(new Label { Text = Strings.GolemioKey, AutoSize = true, Margin = new Padding(0, 4, 0, 2) }, 0, 3);
        _key = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = config.GolemioApiKey ?? "",
            UseSystemPasswordChar = true
        };
        layout.Controls.Add(_key, 0, 4);

        _startWithWindows = new CheckBox
        {
            Text = Strings.StartWithWindows,
            AutoSize = true,
            Checked = Autostart.IsEnabled,
            Margin = new Padding(0, 12, 0, 4)
        };
        layout.Controls.Add(_startWithWindows, 0, 5);

        var update = new Button { Text = Strings.UpdateTimetable, AutoSize = true, Margin = new Padding(0, 12, 0, 8) };
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
        layout.Controls.Add(update, 0, 6);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false
        };
        var ok = new Button { Text = Strings.Ok, DialogResult = DialogResult.OK, AutoSize = true, Padding = new Padding(12, 3, 12, 3) };
        var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(12, 3, 12, 3) };
        ok.Click += (_, _) => Apply();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, 7);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
        RefreshList();
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
