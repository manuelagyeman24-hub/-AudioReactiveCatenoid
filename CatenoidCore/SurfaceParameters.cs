namespace CatenoidCore;

/// <summary>Shape of a single generated frame of the morphing surface.</summary>
public readonly record struct SurfaceParameters(
    double NeckRadius,
    double Height,
    double Morph,
    double RippleAmplitude = 0,
    double RipplePhase = 0,
    double RippleWaves = 6.0)
{
    public static SurfaceParameters Default => new(0.6, 2.4, 0);

    /// <summary>Throws when a value would produce a degenerate surface.</summary>
    public void Validate()
    {
        if (NeckRadius <= 0)
            throw new ArgumentOutOfRangeException(nameof(NeckRadius), NeckRadius, "Neck radius must be positive.");
        if (Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(Height), Height, "Height must be positive.");
    }
}
