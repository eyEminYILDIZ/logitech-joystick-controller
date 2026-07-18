namespace FlightGame.Instruments;

using FlightGame.Joystick;

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
class FlightState
{
    // The twist (yaw) axis on this stick doesn't rest at raw 0 -- it was observed resting
    // around -18918 with +-1000 of counts of jitter. Roll/pitch/yaw are calibrated against
    // whatever they read at startup so deflection is relative to the stick's actual center.
    private const short Deadzone = 1600; // out of +/-32767, absorbs resting jitter after centering
    private const double MaxDeflectionDeg = 45.0;

    private short _rollCenter, _pitchCenter, _yawCenter;
    private bool _calibrated;

    public double PitchDeg { get; private set; }
    public double RollDeg { get; private set; }
    public double YawDeg { get; private set; }
    public double ThrottlePct { get; private set; }
    public double AltitudeFt { get; private set; } = 1000;
    public string Hat { get; private set; } = "CENTER";
    public bool[] Buttons { get; private set; } = [];

    /// <summary>Records the stick's current resting position as the zero point for roll/pitch/yaw.</summary>
    public void Calibrate(JoystickState joystick)
    {
        var (axes, _) = joystick.Snapshot();
        _rollCenter = GetAxis(axes, AxisMap.Roll);
        _pitchCenter = GetAxis(axes, AxisMap.Pitch);
        _yawCenter = GetAxis(axes, AxisMap.Yaw);
        _calibrated = true;
    }

    private static double Normalize(short raw, short center)
    {
        int deflection = raw - center;
        if (Math.Abs(deflection) < Deadzone) return 0;
        return Math.Clamp(deflection / 32767.0, -1.0, 1.0);
    }

    public void Update(JoystickState joystick, double dtSeconds)
    {
        var (axes, buttons) = joystick.Snapshot();
        Buttons = buttons;
        if (!_calibrated) Calibrate(joystick);

        double roll = Normalize(GetAxis(axes, AxisMap.Roll), _rollCenter);
        double pitch = Normalize(GetAxis(axes, AxisMap.Pitch), _pitchCenter);
        double yaw = Normalize(GetAxis(axes, AxisMap.Yaw), _yawCenter);
        double throttleRaw = GetAxis(axes, AxisMap.Throttle);

        RollDeg = roll * MaxDeflectionDeg;
        PitchDeg = -pitch * MaxDeflectionDeg; // stick forward (positive raw) noses down

        // Twist axis is treated as a yaw rate, not an absolute angle.
        YawDeg = Normalize360(YawDeg + yaw * 90.0 * dtSeconds);

        // Slider typically rests full-negative when idle on this stick; remap to 0-100%.
        ThrottlePct = Math.Clamp((throttleRaw + 32767.0) / 65534.0 * 100.0, 0, 100);

        // Toy altitude model: climbs/descends based on throttle above/below a 50% hover point.
        AltitudeFt += (ThrottlePct - 50.0) * 2.0 * dtSeconds;
        if (AltitudeFt < 0) AltitudeFt = 0;

        Hat = DescribeHat(GetAxis(axes, AxisMap.HatX), GetAxis(axes, AxisMap.HatY));
    }

    private static short GetAxis(short[] axes, int index) => index < axes.Length ? axes[index] : (short)0;

    private static double Normalize360(double deg)
    {
        deg %= 360;
        if (deg < 0) deg += 360;
        return deg;
    }

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
