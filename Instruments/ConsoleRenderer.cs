namespace FlightGame.Instruments;

/// <summary>Redraws flight instrument gauges in place (no scrolling/flicker).</summary>
class ConsoleRenderer
{
    // The Extreme 3D Pro has 12 physical buttons; JoystickState pre-allocates a larger
    // generic array, so cap the display here rather than showing empty phantom slots.
    private const int ButtonCount = 12;

    private bool _initialized;

    public void Render(FlightState state, string devicePath, bool connected)
    {
        if (!_initialized)
        {
            Console.Clear();
            Console.CursorVisible = false;
            _initialized = true;
        }

        Console.SetCursorPosition(0, 0);
        WriteLinePadded($"Device: {devicePath}  [{(connected ? "connected" : "DISCONNECTED")}]");
        WriteLinePadded("");
        WriteLinePadded($"Pitch:    {FormatSigned(state.PitchDeg)} deg");
        WriteLinePadded($"Roll:     {FormatSigned(state.RollDeg)} deg");
        WriteLinePadded($"Yaw:      {state.YawDeg,5:000.0} deg");
        WriteLinePadded($"Throttle: {state.ThrottlePct,5:00}%");
        WriteLinePadded($"Altitude: {state.AltitudeFt,7:0} ft");
        WriteLinePadded($"Hat:      {state.Hat}");
        WriteLinePadded("");
        WriteLinePadded(FormatButtons(state.Buttons));
        WriteLinePadded("");
        WriteLinePadded("Ctrl+C to quit.");
    }

    private static string FormatSigned(double v) => (v >= 0 ? "+" : "-") + Math.Abs(v).ToString("00.0");

    private static string FormatButtons(bool[] buttons)
    {
        var parts = new List<string>();
        int count = Math.Min(buttons.Length, ButtonCount);
        for (int i = 0; i < count; i++)
            parts.Add($"{i + 1}:[{(buttons[i] ? "X" : " ")}]");
        return string.Join(" ", parts);
    }

    private void WriteLinePadded(string text)
    {
        int width = Math.Max(Console.WindowWidth - 1, 40);
        if (text.Length > width) text = text[..width];
        Console.Write(text.PadRight(width));
        Console.Write('\n');
    }
}
