using System.Globalization;
using Microsoft.Win32;

namespace Departures;

sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly Form _sync;
    private readonly System.Windows.Forms.Timer _displayTimer;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private AppConfig _config;

    private IReadOnlyList<Departure> _gtfsBoard = [];
    private IReadOnlyList<Departure> _golemioBoard = [];
    private BoardFetchStatus _golemioStatus = BoardFetchStatus.MissingApiKey;
    private HashSet<string> _parsedStops = new(StringComparer.OrdinalIgnoreCase);
    private bool _downloading;
    private bool _refreshing;
    private Icon? _icon;
    private Form? _about;
    private Form? _settings;

    public TrayApplicationContext()
    {
        _config = AppConfig.Load();
        Autostart.ApplyOnLaunch();

        _sync = new Form
        {
            FormBorderStyle = FormBorderStyle.FixedToolWindow,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            Size = new Size(1, 1),
            Opacity = 0,
            ShowIcon = false
        };
        _ = _sync.Handle;

        _menu = new ContextMenuStrip();
        _menu.Opening += (_, _) => RebuildMenu();

        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            ContextMenuStrip = _menu,
            Text = Strings.AppName
        };
        _notifyIcon.MouseClick += OnTrayMouseClick;

        _displayTimer = new System.Windows.Forms.Timer();
        _displayTimer.Tick += (_, _) => OnDisplayTick();

        _pollTimer = new System.Windows.Forms.Timer { Interval = 45_000 };
        _pollTimer.Tick += (_, _) => _ = RefreshAllAsync(forceDownload: false);

        SystemEvents.TimeChanged += OnSystemClockChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        ApplyIcon(null);
        _pollTimer.Start();
        _ = RefreshAllAsync(forceDownload: false);
    }

    private void OnTrayMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        ReloadConfig();
        ShowBalloon(BuildBalloonText());
    }

    private async Task RefreshAllAsync(bool forceDownload)
    {
        if (_refreshing)
            return;

        _refreshing = true;
        try
        {
            ReloadConfig();
            await EnsureGtfsAsync(forceDownload).ConfigureAwait(true);
            await RefreshGolemioAsync().ConfigureAwait(true);
            RefreshPresentation();
        }
        finally
        {
            _refreshing = false;
        }
    }

    private async Task EnsureGtfsAsync(bool forceDownload)
    {
        bool needDownload = forceDownload || !GtfsTimetable.ZipLooksFresh();
        if (needDownload)
        {
            _downloading = !GtfsTimetable.ZipExists;
            if (_downloading)
            {
                RefreshPresentation();
                ShowBalloon(Strings.Downloading);
            }

            bool ok = await GtfsTimetable.DownloadAsync(CancellationToken.None).ConfigureAwait(true);
            _downloading = false;
            if (forceDownload)
                ShowBalloon(ok ? Strings.TimetableReady : Strings.DownloadFailed);
            else if (!ok && !GtfsTimetable.ZipExists)
                ShowBalloon(Strings.DownloadFailed);
        }

        await StopCatalog.EnsureAsync(CancellationToken.None, forceDownload).ConfigureAwait(true);

        HashSet<string> stops = StopIds();
        bool sameStops = stops.SetEquals(_parsedStops);
        if (sameStops && _gtfsBoard.Count > 0 && !forceDownload && !needDownload)
            return;

        IReadOnlyList<Departure> loaded = await Task.Run(() =>
            GtfsTimetable.LoadDepartures(stops, DateTimeOffset.Now)).ConfigureAwait(true);
        _gtfsBoard = loaded;
        _parsedStops = stops;
    }

    private async Task RefreshGolemioAsync()
    {
        WatchConfig? watch = _config.ActiveWatch;
        if (string.IsNullOrWhiteSpace(_config.GolemioApiKey) || watch is null || string.IsNullOrWhiteSpace(watch.StopId))
        {
            _golemioStatus = BoardFetchStatus.MissingApiKey;
            _golemioBoard = [];
            return;
        }

        try
        {
            BoardFetchResult result = await GolemioClient.FetchAsync(
                _config.GolemioApiKey,
                watch.StopId,
                CancellationToken.None).ConfigureAwait(true);

            _golemioStatus = result.Status;
            _golemioBoard = result.Status == BoardFetchStatus.Ok ? result.Departures : [];
        }
        catch
        {
            _golemioStatus = BoardFetchStatus.Failed;
            _golemioBoard = [];
        }
    }

    private HashSet<string> StopIds() =>
        _config.Watches
            .Select(w => w.StopId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private void OnDisplayTick()
    {
        _displayTimer.Stop();
        RefreshPresentation();
    }

    private void RefreshPresentation()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        Departure? next = CurrentDeparture(now);
        int? minutes = next is null ? null : DepartureClock.DisplayedMinutes(next.Time, now);

        ApplyIcon(minutes);
        _notifyIcon.Text = TruncateTip(BuildTooltip(next, minutes));
        ScheduleDisplayTimer(next, now);
    }

    private Departure? CurrentDeparture(DateTimeOffset now)
    {
        WatchConfig? watch = _config.ActiveWatch;
        if (watch is null)
            return null;

        return DepartureSelector.Next(_golemioBoard, watch, now)
            ?? DepartureSelector.Next(_gtfsBoard, watch, now);
    }

    private void ScheduleDisplayTimer(Departure? next, DateTimeOffset now)
    {
        _displayTimer.Stop();
        if (next is null)
            return;

        TimeSpan delay = DepartureClock.DelayUntilNextDisplayChange(next.Time, now);
        if (delay <= TimeSpan.Zero)
            return;

        int ms = (int)Math.Clamp(delay.TotalMilliseconds, 50, 60_000);
        _displayTimer.Interval = ms;
        _displayTimer.Start();
    }

    private string BuildTooltip(Departure? next, int? minutes)
    {
        if (_downloading)
            return Strings.Downloading;

        if (NeedsSetup(out string setup))
            return setup;

        if (next is null || minutes is null)
            return Strings.NoUpcoming;

        return $"{FormatLine(next)}  {FormatClock(next.Time)} {Strings.TooltipMinutes(minutes.Value)}";
    }

    private string BuildBalloonText()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        if (_downloading)
            return Strings.Downloading;

        if (NeedsSetup(out string setup))
            return setup;

        Departure? next = CurrentDeparture(now);
        int? minutes = next is null ? null : DepartureClock.DisplayedMinutes(next.Time, now);
        if (next is null || minutes is null)
            return Strings.NoUpcoming;

        string body = $"{FormatLine(next)}{Environment.NewLine}{FormatClock(next.Time)}  ·  {Strings.BalloonMinutes(minutes.Value)}";
        if (_golemioStatus == BoardFetchStatus.Rejected)
            return $"{body}{Environment.NewLine}{Strings.ApiKeyRejected}";

        return body;
    }

    private bool NeedsSetup(out string message)
    {
        WatchConfig? watch = _config.ActiveWatch;
        if (watch is null || string.IsNullOrWhiteSpace(watch.StopId))
        {
            message = Strings.AddStopInSettings;
            return true;
        }

        message = "";
        return false;
    }

    private static string FormatLine(Departure next)
    {
        if (string.IsNullOrWhiteSpace(next.Headsign))
            return next.RouteShortName;

        return $"{next.RouteShortName} → {next.Headsign}";
    }

    private static string FormatClock(DateTimeOffset time) =>
        time.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture);

    private void ReloadConfig() => _config = AppConfig.Load();

    private void RebuildMenu()
    {
        ReloadConfig();
        _menu.Items.Clear();

        if (_config.Watches.Count == 0)
        {
            _menu.Items.Add(new ToolStripMenuItem(Strings.NoStopConfigured) { Enabled = false });
        }
        else
        {
            WatchConfig? active = _config.ActiveWatch;
            foreach (WatchConfig watch in _config.Watches)
            {
                var item = new ToolStripMenuItem(watch.MenuLabel)
                {
                    Checked = active is not null && string.Equals(watch.Id, active.Id, StringComparison.OrdinalIgnoreCase),
                    Tag = watch,
                    CheckOnClick = false
                };
                item.Click += OnWatchChosen;
                _menu.Items.Add(item);
            }
        }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem(Strings.Settings, null, (_, _) => ShowSettings()));
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem(Strings.About, null, (_, _) => ShowAbout()));
        _menu.Items.Add(new ToolStripMenuItem(Strings.Exit, null, (_, _) => Quit()));
    }

    private void OnWatchChosen(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: WatchConfig watch })
            return;

        _config.ActiveWatchId = watch.Id;
        _config.Save();
        _ = RefreshAllAsync(forceDownload: false);
    }

    private void ShowSettings()
    {
        DialogResult result = TrayDialog.ShowOnce(
            ref _settings,
            _sync,
            () => new SettingsForm(AppConfig.Load(), () => RefreshAllAsync(forceDownload: true)));

        if (result != DialogResult.OK)
            return;

        ReloadConfig();
        _parsedStops.Clear();
        _ = RefreshAllAsync(forceDownload: false);
    }

    private void ShowAbout()
    {
        TrayDialog.ShowOnce(ref _about, _sync, () => new AboutForm());
    }

    private void ShowBalloon(string text)
    {
        _notifyIcon.BalloonTipTitle = Strings.AppName;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.None;
        _notifyIcon.ShowBalloonTip(6000);
    }

    private void ApplyIcon(int? minutes)
    {
        Icon created = TrayIcons.Create(minutes);
        Icon? previous = _icon;
        _icon = created;
        _notifyIcon.Icon = created;
        previous?.Dispose();
    }

    private void OnSystemClockChanged(object? sender, EventArgs e) =>
        BeginInvokeOnTray(() =>
        {
            RefreshPresentation();
            _ = RefreshAllAsync(forceDownload: false);
        });

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            BeginInvokeOnTray(() => _ = RefreshAllAsync(forceDownload: false));
    }

    private void BeginInvokeOnTray(Action action)
    {
        if (_sync.IsHandleCreated)
            _sync.BeginInvoke(action);
        else
            action();
    }

    private static string TruncateTip(string text)
    {
        const int max = 63;
        if (text.Length <= max)
            return text;

        return string.Concat(text.AsSpan(0, max - 1), "…");
    }

    private void Quit()
    {
        SystemEvents.TimeChanged -= OnSystemClockChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _displayTimer.Stop();
        _displayTimer.Dispose();
        _pollTimer.Stop();
        _pollTimer.Dispose();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _icon?.Dispose();
        _menu.Dispose();
        _sync.Dispose();
        Autostart.DeleteInstalledExeAfterThisProcessExits();
        ExitThread();
    }
}
