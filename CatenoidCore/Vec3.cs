namespace CatenoidCore;

/// <summary>Platform independent 3D vector used by the surface generators.</summary>
public readonly struct Vec3 : IEquatable<Vec3>
{
    public Vec3(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public double X { get; }

    public double Y { get; }

    public double Z { get; }

    public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>Distance from the surface axis (the Z axis).</summary>
    public double RadialDistance => Math.Sqrt(X * X + Y * Y);

    public static Vec3 operator -(Vec3 a, Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vec3 operator +(Vec3 a, Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vec3 operator *(Vec3 a, double s) => new(a.X * s, a.Y * s, a.Z * s);

    public static Vec3 Cross(Vec3 a, Vec3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    public bool Equals(Vec3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    public override bool Equals(object? obj) => obj is Vec3 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public override string ToString() => $"({X:0.####}, {Y:0.####}, {Z:0.####})";
}
