using System.Diagnostics;
using Microsoft.Win32;

namespace Departures;

static class Autostart
{
    public const string RunValueName = "PublicTransportDepartures";
    public const string LegacyRunValueName = "Odjezdy";

    private const string RunSubKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedSubKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    public static bool IsEnabled => HasRunValue && !IsDisabledByWindows;

    /// <summary>
    /// The running image cannot be deleted. Remove it after Exit instead.
    /// </summary>
    public static bool DeleteInstalledExeOnExit { get; private set; }

    public static void Enable()
    {
        DeleteInstalledExeOnExit = false;
        InstallCurrentExe();

        using (RegistryKey run = Registry.CurrentUser.CreateSubKey(RunSubKey, writable: true)
               ?? throw new InvalidOperationException("Could not open the Run registry key."))
        {
            run.SetValue(RunValueName, Quoted(AppPaths.InstalledExe));
        }

        SetApproved(enabled: true);
        AppPaths.TryDeleteLegacyInstalledExe();
        DeleteRunValue(LegacyRunValueName);
        DeleteApprovedValue(LegacyRunValueName);
    }

    public static void Disable()
    {
        using (RegistryKey? run = Registry.CurrentUser.OpenSubKey(RunSubKey, writable: true))
        {
            run?.DeleteValue(RunValueName, throwOnMissingValue: false);
            run?.DeleteValue(LegacyRunValueName, throwOnMissingValue: false);
        }

        using (RegistryKey? approved = Registry.CurrentUser.OpenSubKey(ApprovedSubKey, writable: true))
        {
            approved?.DeleteValue(RunValueName, throwOnMissingValue: false);
            approved?.DeleteValue(LegacyRunValueName, throwOnMissingValue: false);
        }

        if (AppPaths.IsRunningFromInstallLocation)
            DeleteInstalledExeOnExit = true;
        else
        {
            TryDeleteInstalledExe();
            AppPaths.TryDeleteLegacyInstalledExe();
        }
    }

    public static void ApplyOnLaunch()
    {
        MigrateLegacyRunValue();
        if (HasRunValue)
        {
            RefreshInstalledCopyIfRegistered();
            AppPaths.TryDeleteLegacyInstalledExe();
            return;
        }

        if (!AppPaths.IsRunningFromInstallLocation)
        {
            TryDeleteInstalledExe();
            AppPaths.TryDeleteLegacyInstalledExe();
        }
    }

    private static void MigrateLegacyRunValue()
    {
        if (!HasRunValueNamed(LegacyRunValueName))
            return;

        bool wasDisabled = IsDisabledByWindowsNamed(LegacyRunValueName);
        Enable();
        if (wasDisabled)
            SetApproved(enabled: false);
    }

    public static void DeleteInstalledExeAfterThisProcessExits()
    {
        if (!DeleteInstalledExeOnExit)
            return;

        string path = AppPaths.InstalledExe;
        Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
            Arguments = $"/c timeout /t 1 /nobreak >nul & del /f /q \"{path}\"",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = false
        });
    }

    public static void RefreshInstalledCopyIfRegistered()
    {
        if (!HasRunValue)
            return;

        if (AppPaths.IsRunningFromInstallLocation)
            return;

        if (!File.Exists(AppPaths.InstalledExe) || InstalledCopyLooksStale())
            InstallCurrentExe();
    }

    private static bool HasRunValue => HasRunValueNamed(RunValueName);

    private static bool HasRunValueNamed(string name)
    {
        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(RunSubKey);
        return run?.GetValue(name) is string { Length: > 0 };
    }

    private static bool IsDisabledByWindows => IsDisabledByWindowsNamed(RunValueName);

    private static bool IsDisabledByWindowsNamed(string name)
    {
        using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(ApprovedSubKey);
        if (approved?.GetValue(name) is not byte[] data || data.Length == 0)
            return false;

        byte flag = data[0];
        return flag is 0x03 or 0x07;
    }

    private static void DeleteRunValue(string name)
    {
        using RegistryKey? run = Registry.CurrentUser.OpenSubKey(RunSubKey, writable: true);
        run?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void DeleteApprovedValue(string name)
    {
        using RegistryKey? approved = Registry.CurrentUser.OpenSubKey(ApprovedSubKey, writable: true);
        approved?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void SetApproved(bool enabled)
    {
        using RegistryKey approved = Registry.CurrentUser.CreateSubKey(ApprovedSubKey, writable: true)
            ?? throw new InvalidOperationException("Could not open the StartupApproved registry key.");

        byte flag = enabled ? (byte)0x02 : (byte)0x03;
        var data = new byte[12];
        data[0] = flag;
        approved.SetValue(RunValueName, data, RegistryValueKind.Binary);
    }

    private static void InstallCurrentExe()
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);
        if (AppPaths.IsRunningFromInstallLocation)
            return;

        File.Copy(AppPaths.CurrentExe, AppPaths.InstalledExe, overwrite: true);
    }

    private static bool InstalledCopyLooksStale()
    {
        var current = new FileInfo(AppPaths.CurrentExe);
        var installed = new FileInfo(AppPaths.InstalledExe);
        return current.Length != installed.Length
            || current.LastWriteTimeUtc != installed.LastWriteTimeUtc;
    }

    private static void TryDeleteInstalledExe()
    {
        try
        {
            if (File.Exists(AppPaths.InstalledExe))
                File.Delete(AppPaths.InstalledExe);
        }
        catch
        {
        }
    }

    private static string Quoted(string path) => $"\"{path}\"";
}
