namespace CatenoidCore;

/// <summary>Texture coordinate pair.</summary>
public readonly struct Vec2
{
    public Vec2(double u, double v)
    {
        U = u;
        V = v;
    }

    public double U { get; }

    public double V { get; }

    public override string ToString() => $"({U:0.####}, {V:0.####})";
}
