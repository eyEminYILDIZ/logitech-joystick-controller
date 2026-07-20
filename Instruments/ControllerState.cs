namespace OpenCms.Libraries.InputController.JoySticks.LogitechExtreme3dPro.Instruments;

using OpenCms.Libraries.InputController.JoySticks.LogitechExtreme3dPro.Joystick;

/// <summary>
/// Axis/button layout for a Logitech Extreme 3D Pro (USB 046d:c215) under Linux's joydev
/// driver, confirmed against the kernel's evdev ABS bitmap on real hardware: ABS_X, ABS_Y,
/// ABS_RZ, ABS_THROTTLE, ABS_HAT0X, ABS_HAT0Y map to js axes 0-5 in that order, and the
/// 12 BTN_JOYSTICK/BTN_BASE* buttons map to js buttons 0-11.
/// </summary>
static class AxisMap
{
    public const int Roll = 0;      // stick left/right
    public const int Pitch = 1;     // stick forward/back
    public const int Yaw = 2;       // stick twist
    public const int Throttle = 3;  // throttle slider
    public const int HatX = 4;      // POV hat, horizontal
    public const int HatY = 5;      // POV hat, vertical
}

/// <summary>Derived flight instrument readings, updated each tick from raw joystick state.</summary>
class ControllerState
{
    // The twist (yaw) axis on this stick doesn't rest at raw 0 -- it was observed resting
    // around -18918 with +-1000 of counts of jitter. Roll/pitch/yaw are calibrated against
    // whatever they read at startup so deflection is relative to the stick's actual center.
    private const short Deadzone = 1600; // out of +/-32767, absorbs resting jitter after centering
    private const double MaxDeflectionDeg = 45.0;

    private readonly AxisCalibration _rollCal = new();
    private readonly AxisCalibration _pitchCal = new();
    private readonly AxisCalibration _yawCal = new();
    private bool _calibrated;

    public double PitchDeg { get; private set; }
    public double RollDeg { get; private set; }
    public double YawDeg { get; private set; }
    public double ThrottlePct { get; private set; }
    public string Hat { get; private set; } = "CENTER";
    public bool[] Buttons { get; private set; } = [];

    /// <summary>Records the stick's current resting position as the zero point for roll/pitch/yaw.</summary>
    public void Calibrate(JoystickState joystick)
    {
        var (axes, _) = joystick.Snapshot();
        _rollCal.SetCenter(GetAxis(axes, AxisMap.Roll));
        _pitchCal.SetCenter(GetAxis(axes, AxisMap.Pitch));
        _yawCal.SetCenter(GetAxis(axes, AxisMap.Yaw));
        _calibrated = true;
    }

    public void Update(JoystickState joystick)
    {
        var (axes, buttons) = joystick.Snapshot();
        Buttons = buttons;
        if (!_calibrated) Calibrate(joystick);

        double roll = _rollCal.Normalize(GetAxis(axes, AxisMap.Roll), Deadzone);
        double pitch = _pitchCal.Normalize(GetAxis(axes, AxisMap.Pitch), Deadzone);
        double yaw = _yawCal.Normalize(GetAxis(axes, AxisMap.Yaw), Deadzone);
        double throttleRaw = GetAxis(axes, AxisMap.Throttle);

        RollDeg = roll * MaxDeflectionDeg;
        PitchDeg = pitch * MaxDeflectionDeg; // reversed: stick forward (positive raw) noses up

        // Direct deflection, like pitch/roll: snaps back toward 0 as soon as the twist is centered.
        YawDeg = yaw * MaxDeflectionDeg;

        // Reversed: slider's raw-negative end (was 0%) now maps to 100%, and vice versa.
        ThrottlePct = Math.Clamp((32767.0 - throttleRaw) / 65534.0 * 100.0, 0, 100);

        Hat = DescribeHat(GetAxis(axes, AxisMap.HatX), GetAxis(axes, AxisMap.HatY));
    }

    private static short GetAxis(short[] axes, int index) => index < axes.Length ? axes[index] : (short)0;

    private static string DescribeHat(short x, short y)
    {
        int hx = Math.Sign(x);
        int hy = Math.Sign(y);
        return (hx, hy) switch
        {
            (0, 0) => "CENTER",
            (0, -1) => "UP",
            (0, 1) => "DOWN",
            (-1, 0) => "LEFT",
            (1, 0) => "RIGHT",
            (-1, -1) => "UP-LEFT",
            (1, -1) => "UP-RIGHT",
            (-1, 1) => "DOWN-LEFT",
            (1, 1) => "DOWN-RIGHT",
            _ => "CENTER",
        };
    }
}
