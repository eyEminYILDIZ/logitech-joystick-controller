using FlightGame.Instruments;
using FlightGame.Joystick;

string? devicePath = null;
bool rawMode = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--device":
            if (i + 1 < args.Length) devicePath = args[++i];
            break;
        case "--raw":
            rawMode = true;
            break;
        case "--list-devices":
            var found = JoystickDevice.ListAll().ToList();
            Console.WriteLine(found.Count == 0 ? "No /dev/input/jsN devices found." : string.Join('\n', found));
            return;
        case "-h":
        case "--help":
            Console.WriteLine("Usage: dotnet run -- [--device /dev/input/jsN] [--raw] [--list-devices]");
            return;
    }
}

devicePath ??= JoystickDevice.Find("Logitech Extreme 3D") ?? JoystickDevice.ListAll().FirstOrDefault();

if (devicePath is null)
{
    Console.WriteLine("No joystick device found under /dev/input/js*.");
    Console.WriteLine("Plug in the joystick, then try --list-devices or pass --device /dev/input/jsN explicitly.");
    Console.WriteLine("If the device exists but can't be opened, your user may need to be in the 'input' group:");
    Console.WriteLine("  sudo usermod -aG input $USER   (then log out/in)");
    return;
}

Console.WriteLine($"Opening {devicePath} ...");

JoystickReader reader;
try
{
    reader = new JoystickReader(devicePath);
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to open {devicePath}: {ex.Message}");
    return;
}

bool running = true;
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    running = false;
};

if (rawMode)
{
    Console.WriteLine("Raw mode: move each axis/button/hat to see its index. Ctrl+C to quit.");
    var (prevAxes, prevButtons) = reader.State.Snapshot();
    while (running)
    {
        Thread.Sleep(30);
        var (axes, buttons) = reader.State.Snapshot();
        for (int i = 0; i < axes.Length; i++)
        {
            if (i >= prevAxes.Length || axes[i] != prevAxes[i])
                Console.WriteLine($"axis[{i}] = {axes[i]}");
        }
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i >= prevButtons.Length || buttons[i] != prevButtons[i])
                Console.WriteLine($"button[{i}] = {buttons[i]}");
        }
        prevAxes = axes;
        prevButtons = buttons;

        if (!reader.IsConnected)
        {
            Console.WriteLine($"Device disconnected. {reader.LastError}");
            break;
        }
    }
}
else
{
    Console.WriteLine("Calibrating... keep the stick centered and let go of the twist axis.");
    Thread.Sleep(300); // let the kernel's initial-state event replay settle
    var flightState = new FlightState();
    flightState.Calibrate(reader.State);
    var renderer = new ConsoleRenderer();
    var sw = System.Diagnostics.Stopwatch.StartNew();
    double last = 0;

    try
    {
        while (running)
        {
            double now = sw.Elapsed.TotalSeconds;
            double dt = Math.Clamp(now - last, 0.001, 0.5);
            last = now;

            flightState.Update(reader.State, dt);
            renderer.Render(flightState, devicePath, reader.IsConnected);

            if (!reader.IsConnected)
            {
                Console.SetCursorPosition(0, 14);
                Console.WriteLine($"Device disconnected: {reader.LastError}");
                break;
            }

            Thread.Sleep(33); // ~30Hz
        }
    }
    finally
    {
        Console.CursorVisible = true;
    }
}

reader.Dispose();
