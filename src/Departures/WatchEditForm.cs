using System.Diagnostics;

namespace Departures;

sealed class WatchEditForm : Form
{
    private readonly TextBox _label;
    private readonly TextBox _search;
    private readonly ListBox _matches;
    private readonly ComboBox _pillar;
    private readonly ComboBox _line;
    private readonly ComboBox _to;
    private readonly TextBox _stopIdFallback;
    private readonly Label _status;
    private bool _ready;

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
        MinimumSize = new Size(460, 0);

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 1,
            Dock = DockStyle.Fill
        };

        _label = AddText(layout, Strings.WatchLabel, watch.Label ?? "");
        _search = AddText(layout, Strings.WatchSearch, "");
        _search.TextChanged += SearchHandler;

        layout.Controls.Add(Caption(Strings.WatchMatches));
        _matches = new ListBox
        {
            Width = 400,
            Height = 110,
            IntegralHeight = false
        };
        _matches.SelectedIndexChanged += (_, _) => FillPillars();
        layout.Controls.Add(_matches);

        _pillar = AddCombo(layout, Strings.WatchPillar);
        _pillar.SelectedIndexChanged += (_, _) => FillLinesAndDestinations();
        _line = AddCombo(layout, Strings.WatchLine);
        _to = AddCombo(layout, Strings.WatchTo);
        _stopIdFallback = AddText(layout, Strings.WatchStopIdFallback, watch.StopId ?? "");

        _status = new Label
        {
            Text = Strings.WatchHint,
            AutoSize = true,
            ForeColor = SystemColors.GrayText,
            MaximumSize = new Size(400, 0),
            Margin = new Padding(0, 8, 0, 8)
        };
        layout.Controls.Add(_status);

        var map = new LinkLabel { Text = Strings.StopMap, AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
        map.LinkClicked += (_, _) => OpenUrl(Strings.StopMapUrl);
        layout.Controls.Add(map);

        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            WrapContents = false
        };
        var ok = new Button { Text = Strings.Ok, AutoSize = true, Padding = new Padding(12, 3, 12, 3) };
        var cancel = new Button { Text = Strings.Cancel, DialogResult = DialogResult.Cancel, AutoSize = true, Padding = new Padding(12, 3, 12, 3) };
        ok.Click += (_, _) => TryAccept();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons);

        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
        Shown += async (_, _) => await LoadCatalogAsync();
    }

    private async Task LoadCatalogAsync()
    {
        _status.Text = Strings.LoadingStops;
        bool ok = await StopCatalog.EnsureAsync(CancellationToken.None).ConfigureAwait(true);
        _ready = ok;
        _status.Text = ok ? Strings.WatchHint : Strings.StopsLoadFailed;
        if (!ok)
        {
            if (string.IsNullOrWhiteSpace(_stopIdFallback.Text))
                _stopIdFallback.Text = Result.StopId ?? "";
            return;
        }

        if (!string.IsNullOrWhiteSpace(Result.StopId))
        {
            StopGroup? group = StopCatalog.FindGroupByStopId(Result.StopId);
            if (group is not null)
            {
                _search.TextChanged -= SearchHandler;
                _search.Text = group.Name;
                _search.TextChanged += SearchHandler;
                ApplySearch();
                SelectMatch(group);
                SelectPillar(Result.StopId);
                SelectCombo(_line, Result.RouteShortName);
                SelectCombo(_to, Result.HeadsignContains);
                return;
            }
        }

        ApplySearch();
    }

    private void SearchHandler(object? sender, EventArgs e) => ApplySearch();

    private void ApplySearch()
    {
        if (!_ready)
            return;

        object? keep = _matches.SelectedItem;
        _matches.BeginUpdate();
        _matches.Items.Clear();
        foreach (StopGroup group in StopCatalog.Search(_search.Text))
            _matches.Items.Add(group);
        _matches.EndUpdate();

        if (keep is StopGroup previous)
        {
            for (int i = 0; i < _matches.Items.Count; i++)
            {
                if (_matches.Items[i] is StopGroup g && g.Name == previous.Name && g.Municipality == previous.Municipality)
                {
                    _matches.SelectedIndex = i;
                    return;
                }
            }
        }

        if (_matches.Items.Count > 0)
            _matches.SelectedIndex = 0;
        else
            FillPillars();
    }

    private void SelectMatch(StopGroup group)
    {
        for (int i = 0; i < _matches.Items.Count; i++)
        {
            if (_matches.Items[i] is StopGroup g && g.Name == group.Name && g.Municipality == group.Municipality)
            {
                _matches.SelectedIndex = i;
                return;
            }
        }
    }

    private void FillPillars()
    {
        _pillar.Items.Clear();
        if (_matches.SelectedItem is not StopGroup group)
        {
            FillLinesAndDestinations();
            return;
        }

        foreach (StopPillar pillar in group.Pillars)
            _pillar.Items.Add(pillar);

        if (_pillar.Items.Count > 0)
            _pillar.SelectedIndex = 0;
        else
            FillLinesAndDestinations();
    }

    private void SelectPillar(string stopId)
    {
        for (int i = 0; i < _pillar.Items.Count; i++)
        {
            if (_pillar.Items[i] is StopPillar p && string.Equals(p.GtfsId, stopId, StringComparison.OrdinalIgnoreCase))
            {
                _pillar.SelectedIndex = i;
                return;
            }
        }
    }

    private void FillLinesAndDestinations()
    {
        string? line = ComboValue(_line);
        string? dest = ComboValue(_to);
        _line.Items.Clear();
        _to.Items.Clear();
        _line.Items.Add(new ComboOption("", Strings.AnyLine));
        _to.Items.Add(new ComboOption("", Strings.AnyDestination));

        if (_pillar.SelectedItem is StopPillar pillar)
        {
            _stopIdFallback.Text = pillar.GtfsId;
            foreach (string name in pillar.Lines)
                _line.Items.Add(new ComboOption(name, name));
            foreach (string name in pillar.Destinations)
                _to.Items.Add(new ComboOption(name, name));
        }

        SelectCombo(_line, line);
        SelectCombo(_to, dest);
        if (_line.SelectedIndex < 0)
            _line.SelectedIndex = 0;
        if (_to.SelectedIndex < 0)
            _to.SelectedIndex = 0;
    }

    private static void SelectCombo(ComboBox box, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (box.Items.Count > 0)
                box.SelectedIndex = 0;
            return;
        }

        for (int i = 0; i < box.Items.Count; i++)
        {
            if (box.Items[i] is ComboOption opt && string.Equals(opt.Value, value, StringComparison.OrdinalIgnoreCase))
            {
                box.SelectedIndex = i;
                return;
            }
        }
    }

    private static string? ComboValue(ComboBox box) =>
        box.SelectedItem is ComboOption opt && opt.Value.Length > 0 ? opt.Value : null;

    private void TryAccept()
    {
        string stopId = _stopIdFallback.Text.Trim();
        if (string.IsNullOrWhiteSpace(stopId) && _pillar.SelectedItem is StopPillar pillar)
            stopId = pillar.GtfsId;
        if (string.IsNullOrWhiteSpace(stopId))
        {
            MessageBox.Show(this, Strings.StopIdRequired, Strings.EditWatch, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Result.StopId = stopId;
        Result.RouteShortName = ComboValue(_line) ?? "";
        Result.HeadsignContains = ComboValue(_to) ?? "";
        Result.Label = string.IsNullOrWhiteSpace(_label.Text)
            ? BuildLabel()
            : _label.Text.Trim();
        if (string.IsNullOrWhiteSpace(Result.Id))
            Result.Id = Guid.NewGuid().ToString("N")[..10];

        DialogResult = DialogResult.OK;
        Close();
    }

    private string BuildLabel()
    {
        string place = _matches.SelectedItem is StopGroup group ? group.Name : pillarName();
        string line = ComboValue(_line) ?? "";
        return string.IsNullOrWhiteSpace(line) ? place : $"{place} · {line}";

        string pillarName() => _pillar.SelectedItem is StopPillar p ? p.GtfsId : "";
    }

    private static Label Caption(string text) =>
        new() { Text = text, AutoSize = true, Margin = new Padding(0, 8, 0, 2) };

    private static TextBox AddText(TableLayoutPanel layout, string caption, string value)
    {
        layout.Controls.Add(Caption(caption));
        var box = new TextBox { Text = value, Width = 400, Margin = new Padding(0, 0, 0, 4) };
        layout.Controls.Add(box);
        return box;
    }

    private static ComboBox AddCombo(TableLayoutPanel layout, string caption)
    {
        layout.Controls.Add(Caption(caption));
        var box = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 400,
            Margin = new Padding(0, 0, 0, 4)
        };
        layout.Controls.Add(box);
        return box;
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

    private readonly record struct ComboOption(string Value, string Text)
    {
        public override string ToString() => Text;
    }
}
