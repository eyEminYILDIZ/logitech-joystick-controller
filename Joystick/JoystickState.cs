namespace LogitechJoystickController.Joystick;

/// <summary>Thread-safe latest snapshot of a joystick's axes and buttons.</summary>
class JoystickState
{
    private readonly object _lock = new();
    private short[] _axes = new short[8];
    private bool[] _buttons = new bool[16];

    public void SetAxis(int index, short value)
    {
        lock (_lock)
        {
            if (index >= _axes.Length) Array.Resize(ref _axes, index + 1);
            _axes[index] = value;
        }
    }

    public void SetButton(int index, bool pressed)
    {
        lock (_lock)
        {
            if (index >= _buttons.Length) Array.Resize(ref _buttons, index + 1);
            _buttons[index] = pressed;
        }
    }

    public (short[] Axes, bool[] Buttons) Snapshot()
    {
        lock (_lock)
        {
            return ((short[])_axes.Clone(), (bool[])_buttons.Clone());
        }
    }
}
