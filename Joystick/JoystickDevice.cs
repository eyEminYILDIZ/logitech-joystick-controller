namespace OpenCms.Libraries.InputController.JoySticks.LogitechExtreme3dPro.Joystick;

/// <summary>Resolves which /dev/input/jsN node belongs to a given device name.</summary>
static class JoystickDevice
{
    private const string ProcDevices = "/proc/bus/input/devices";

    /// <summary>
    /// Scans /proc/bus/input/devices for a device whose name contains nameHint
    /// (case-insensitive) and returns the /dev/input/jsN path from its Handlers line.
    /// Returns null if nothing matched.
    /// </summary>
    public static string? Find(string nameHint)
    {
        if (!File.Exists(ProcDevices)) return null;

        string? currentName = null;
        foreach (var line in File.ReadLines(ProcDevices))
        {
            if (line.StartsWith("N: Name="))
            {
                currentName = line["N: Name=".Length..].Trim('"');
            }
            else if (line.StartsWith("H: Handlers="))
            {
                if (currentName != null &&
                    currentName.Contains(nameHint, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var token in line["H: Handlers=".Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (token.StartsWith("js"))
                            return $"/dev/input/{token}";
                    }
                }
            }
            else if (line.Length == 0)
            {
                currentName = null;
            }
        }
        return null;
    }

    /// <summary>Lists every jsN device currently present, for diagnostics.</summary>
    public static IEnumerable<string> ListAll()
    {
        var dir = "/dev/input";
        if (!Directory.Exists(dir)) yield break;
        foreach (var path in Directory.EnumerateFiles(dir, "js*").OrderBy(p => p))
            yield return path;
    }
}
