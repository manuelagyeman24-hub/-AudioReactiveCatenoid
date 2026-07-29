namespace CatenoidCore;

/// <summary>
/// Closed-form geometry of the catenoid and its isometric partner, the helicoid.
/// All functions are pure so they can be unit tested without a rendering stack.
/// </summary>
public static class CatenoidMath
{
    /// <summary>Pitch of the helicoid relative to the catenoid neck radius.</summary>
    public const double HelicoidPitch = 0.2;

    /// <summary>Radius of the catenoid at height <paramref name="z"/> for neck radius <paramref name="a"/>.</summary>
    public static double CatenoidRadius(double z, double a)
    {
        if (a <= 0) throw new ArgumentOutOfRangeException(nameof(a), a, "Neck radius must be positive.");
        return a * Math.Cosh(z / a);
    }

    /// <summary>Point on the catenoid: (a·cosh(z/a)·cosθ, a·cosh(z/a)·sinθ, z).</summary>
    public static Vec3 CatenoidPoint(double theta, double z, double a)
    {
        double r = CatenoidRadius(z, a);
        return new Vec3(r * Math.Cos(theta), r * Math.Sin(theta), z);
    }

    /// <summary>Point on the associated helicoid: (z·cosθ, z·sinθ, a·θ·pitch).</summary>
    public static Vec3 HelicoidPoint(double theta, double z, double a)
    {
        return new Vec3(z * Math.Cos(theta), z * Math.Sin(theta), a * theta * HelicoidPitch);
    }

    /// <summary>
    /// Linear blend between the catenoid (<paramref name="morph"/> = 0) and the helicoid
    /// (<paramref name="morph"/> = 1), with an optional radial ripple used for audio reactivity.
    /// </summary>
    /// <param name="theta">Angle around the axis, in radians.</param>
    /// <param name="z">Height along the axis.</param>
    /// <param name="a">Catenoid neck radius.</param>
    /// <param name="morph">Blend amount, clamped to [0, 1].</param>
    /// <param name="rippleAmplitude">Radial ripple amplitude (0 disables the ripple).</param>
    /// <param name="ripplePhase">Ripple animation phase, in radians.</param>
    /// <param name="rippleWaves">Number of ripple lobes around the axis.</param>
    public static Vec3 MorphPoint(
        double theta,
        double z,
        double a,
        double morph,
        double rippleAmplitude = 0,
        double ripplePhase = 0,
        double rippleWaves = 6.0)
    {
        morph = Math.Clamp(morph, 0.0, 1.0);

        Vec3 catenoid = CatenoidPoint(theta, z, a);
        Vec3 helicoid = HelicoidPoint(theta, z, a);

        double x = (1 - morph) * catenoid.X + morph * helicoid.X;
        double y = (1 - morph) * catenoid.Y + morph * helicoid.Y;
        double zz = (1 - morph) * catenoid.Z + morph * helicoid.Z;

        if (rippleAmplitude != 0)
        {
            double scale = 1.0 + rippleAmplitude * RippleFactor(theta, z, ripplePhase, rippleWaves);
            x *= scale;
            y *= scale;
        }

        return new Vec3(x, y, zz);
    }

    /// <summary>Ripple displacement in [-1, 1] applied radially to the surface.</summary>
    public static double RippleFactor(double theta, double z, double phase, double waves = 6.0)
    {
        return Math.Sin(waves * theta + phase * 2.0) * Math.Cos(3.0 * z - phase);
    }
}
