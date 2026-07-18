namespace FlightGame.Instruments;

/// <summary>
/// Per-axis center + auto-expanding min/max, so a self-centering stick axis maps to a
/// symmetric +-1 range even when its resting position isn't at the hardware's true
/// midpoint (which leaves more raw travel available on one side than the other).
/// The positive and negative sides are scaled independently against whatever the largest
/// deflection observed on that side has been so far, expanding as further extremes are hit.
/// </summary>
class AxisCalibration
{
    // Reasonable starting scale so a first small movement doesn't read as full deflection
    // before the real physical extreme on that side has been observed even once.
    private const int InitialRange = 20000;

    private short _center;
    private int _maxPositive = InitialRange;
    private int _maxNegative = InitialRange;

    public void SetCenter(short raw) => _center = raw;

    public double Normalize(short raw, short deadzone)
    {
        int deflection = raw - _center;

        if (deflection > _maxPositive) _maxPositive = deflection;
        if (-deflection > _maxNegative) _maxNegative = -deflection;

        if (Math.Abs(deflection) < deadzone) return 0;

        return deflection >= 0
            ? Math.Clamp((double)deflection / _maxPositive, 0, 1)
            : -Math.Clamp((double)-deflection / _maxNegative, 0, 1);
    }
}
