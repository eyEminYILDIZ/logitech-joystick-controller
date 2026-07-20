namespace LogitechJoystickController.Joystick;

/// <summary>
/// Reads the Linux joydev binary protocol (struct js_event, 8 bytes per event:
/// int32 time, int16 value, uint8 type, uint8 number) from /dev/input/jsN on a
/// background thread and keeps a JoystickState up to date.
/// </summary>
class JoystickReader : IDisposable
{
    private const byte JsEventButton = 0x01;
    private const byte JsEventAxis = 0x02;
    private const byte JsEventInit = 0x80; // OR'd in when the kernel replays initial state

    public JoystickState State { get; } = new();
    public string DevicePath { get; }
    public bool IsConnected { get; private set; } = true;
    public string? LastError { get; private set; }

    private readonly Thread _thread;
    private volatile bool _stop;

    public JoystickReader(string devicePath)
    {
        DevicePath = devicePath;
        _thread = new Thread(ReadLoop) { IsBackground = true, Name = "JoystickReader" };
        _thread.Start();
    }

    private void ReadLoop()
    {
        try
        {
            using var stream = new FileStream(DevicePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            Span<byte> buffer = stackalloc byte[8];

            while (!_stop)
            {
                int totalRead = 0;
                while (totalRead < 8)
                {
                    int n = stream.Read(buffer[totalRead..]);
                    if (n == 0)
                    {
                        // Device closed/unplugged.
                        IsConnected = false;
                        return;
                    }
                    totalRead += n;
                }

                short value = (short)(buffer[4] | (buffer[5] << 8));
                byte type = buffer[6];
                byte number = buffer[7];
                byte baseType = (byte)(type & ~JsEventInit);

                if (baseType == JsEventAxis)
                    State.SetAxis(number, value);
                else if (baseType == JsEventButton)
                    State.SetButton(number, value != 0);
            }
        }
        catch (Exception ex)
        {
            IsConnected = false;
            LastError = ex.Message;
        }
    }

    public void Dispose()
    {
        _stop = true;
    }
}
