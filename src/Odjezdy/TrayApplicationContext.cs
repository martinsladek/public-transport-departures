using System.Globalization;
using Microsoft.Win32;

namespace Odjezdy;

sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly Form _sync;
    private readonly System.Windows.Forms.Timer _displayTimer;
    private readonly System.Windows.Forms.Timer _pollTimer;
    private AppConfig _config;

    private IReadOnlyList<Departure> _board = [];
    private BoardFetchStatus _lastStatus = BoardFetchStatus.Failed;
    private DateTimeOffset? _lastOkAt;
    private bool _fetching;
    private Icon? _icon;

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
        _pollTimer.Tick += (_, _) => _ = RefreshFromApiAsync();

        SystemEvents.TimeChanged += OnSystemClockChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        ApplyIcon(null);
        _pollTimer.Start();
        _ = RefreshFromApiAsync();
    }

    private void OnTrayMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        ReloadConfig();
        if (_board.Count == 0 && !string.IsNullOrWhiteSpace(_config.GolemioApiKey))
            _ = RefreshFromApiAsync();

        ShowBalloon(BuildBalloonText());
    }

    private async Task RefreshFromApiAsync()
    {
        if (_fetching)
            return;

        ReloadConfig();
        WatchConfig? watch = _config.ActiveWatch;
        if (string.IsNullOrWhiteSpace(_config.GolemioApiKey))
        {
            _lastStatus = BoardFetchStatus.MissingApiKey;
            RefreshPresentation();
            return;
        }

        if (watch is null || string.IsNullOrWhiteSpace(watch.StopId))
        {
            _lastStatus = BoardFetchStatus.Failed;
            RefreshPresentation();
            return;
        }

        _fetching = true;
        try
        {
            BoardFetchResult result = await GolemioClient.FetchAsync(
                _config.GolemioApiKey,
                watch.StopId,
                CancellationToken.None);

            if (result.Status == BoardFetchStatus.Ok)
            {
                _board = result.Departures;
                _lastOkAt = DateTimeOffset.Now;
            }

            _lastStatus = result.Status;
        }
        catch
        {
            _lastStatus = BoardFetchStatus.Failed;
        }
        finally
        {
            _fetching = false;
            RefreshPresentation();
        }
    }

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
        _notifyIcon.Text = TruncateTip(BuildTooltip(next, minutes, now));
        ScheduleDisplayTimer(next, now);
    }

    private Departure? CurrentDeparture(DateTimeOffset now)
    {
        WatchConfig? watch = _config.ActiveWatch;
        if (watch is null)
            return null;

        return DepartureSelector.Next(_board, watch, now);
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

    private string BuildTooltip(Departure? next, int? minutes, DateTimeOffset now)
    {
        if (NeedsSetup(out string setup))
            return setup;

        if (next is null || minutes is null)
            return WithOffline(Strings.NoUpcoming, now);

        return WithOffline(
            $"{FormatLine(next)}  {FormatClock(next.Time)} {Strings.TooltipMinutes(minutes.Value)}",
            now);
    }

    private string BuildBalloonText()
    {
        DateTimeOffset now = DateTimeOffset.Now;
        if (NeedsSetup(out string setup))
            return $"{setup}{Environment.NewLine}{Environment.NewLine}{Strings.ConfigPathPrefix}{Environment.NewLine}{AppPaths.ConfigFile}";

        Departure? next = CurrentDeparture(now);
        int? minutes = next is null ? null : DepartureClock.DisplayedMinutes(next.Time, now);
        if (next is null || minutes is null)
            return WithOffline(Strings.NoUpcoming, now);

        return WithOffline(
            $"{FormatLine(next)}{Environment.NewLine}{FormatClock(next.Time)}  ·  {Strings.BalloonMinutes(minutes.Value)}",
            now);
    }

    private bool NeedsSetup(out string message)
    {
        if (string.IsNullOrWhiteSpace(_config.GolemioApiKey))
        {
            message = Strings.MissingApiKey;
            return true;
        }

        WatchConfig? watch = _config.ActiveWatch;
        if (watch is null || string.IsNullOrWhiteSpace(watch.StopId))
        {
            message = Strings.MissingStop;
            return true;
        }

        if (_lastStatus == BoardFetchStatus.Rejected && _lastOkAt is null)
        {
            message = Strings.ApiKeyRejected;
            return true;
        }

        if (_lastStatus == BoardFetchStatus.Failed && _lastOkAt is null)
        {
            message = Strings.RefreshFailed;
            return true;
        }

        message = "";
        return false;
    }

    private string WithOffline(string text, DateTimeOffset now)
    {
        if (_lastStatus == BoardFetchStatus.Ok)
            return text;

        if (_lastOkAt is DateTimeOffset ok && CurrentDeparture(now) is not null)
            return $"{text} · {Strings.Offline}";

        return _lastStatus == BoardFetchStatus.Rejected ? Strings.ApiKeyRejected : text;
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
        _menu.Items.Add(new ToolStripMenuItem(Strings.Settings) { Enabled = false });
        _menu.Items.Add(new ToolStripMenuItem(Strings.StartWithWindows, null, (_, _) => ToggleAutostart())
        {
            Checked = Autostart.IsEnabled,
            CheckOnClick = false
        });
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
        _ = RefreshFromApiAsync();
    }

    private void ToggleAutostart()
    {
        try
        {
            if (Autostart.IsEnabled)
                Autostart.Disable();
            else
                Autostart.Enable();
        }
        catch
        {
            ShowBalloon(Strings.AutostartFailed);
        }
    }

    private void ShowAbout()
    {
        using var about = new AboutForm();
        about.ShowDialog();
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
            _ = RefreshFromApiAsync();
        });

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
            BeginInvokeOnTray(() => _ = RefreshFromApiAsync());
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
