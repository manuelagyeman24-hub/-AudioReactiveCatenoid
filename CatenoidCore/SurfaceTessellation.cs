namespace CatenoidCore;

/// <summary>
/// Fixed topology tessellation of the morphing surface. Triangle indices and texture
/// coordinates depend only on the grid resolution, so they are computed once and the
/// per-frame work is reduced to writing positions into a caller owned buffer.
/// </summary>
public sealed class SurfaceTessellation
{
    public const int MinSteps = 4;
    public const int MaxSteps = 400;

    public SurfaceTessellation(int uSteps, int vSteps)
    {
        if (uSteps is < MinSteps or > MaxSteps)
            throw new ArgumentOutOfRangeException(nameof(uSteps), uSteps, $"uSteps must be within [{MinSteps}, {MaxSteps}].");
        if (vSteps is < MinSteps or > MaxSteps)
            throw new ArgumentOutOfRangeException(nameof(vSteps), vSteps, $"vSteps must be within [{MinSteps}, {MaxSteps}].");

        USteps = uSteps;
        VSteps = vSteps;

        TriangleIndices = BuildIndices(uSteps, vSteps);
        TextureCoordinates = BuildTextureCoordinates(uSteps, vSteps);
    }

    /// <summary>Number of divisions around the axis.</summary>
    public int USteps { get; }

    /// <summary>Number of divisions along the axis.</summary>
    public int VSteps { get; }

    public int VertexCount => (USteps + 1) * (VSteps + 1);

    public int TriangleCount => USteps * VSteps * 2;

    public int[] TriangleIndices { get; }

    public Vec2[] TextureCoordinates { get; }

    public Vec3[] CreatePositionBuffer() => new Vec3[VertexCount];

    /// <summary>Writes the surface for <paramref name="parameters"/> into <paramref name="positions"/>.</summary>
    public void FillPositions(Vec3[] positions, SurfaceParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(positions);
        if (positions.Length != VertexCount)
            throw new ArgumentException($"Buffer must hold exactly {VertexCount} positions.", nameof(positions));

        parameters.Validate();

        int index = 0;
        for (int v = 0; v <= VSteps; v++)
        {
            double z = parameters.Height * (v / (double)VSteps - 0.5);
            for (int u = 0; u <= USteps; u++)
            {
                double theta = 2.0 * Math.PI * u / USteps;
                positions[index++] = CatenoidMath.MorphPoint(
                    theta,
                    z,
                    parameters.NeckRadius,
                    parameters.Morph,
                    parameters.RippleAmplitude,
                    parameters.RipplePhase,
                    parameters.RippleWaves);
            }
        }
    }

    private static int[] BuildIndices(int uSteps, int vSteps)
    {
        int[] indices = new int[uSteps * vSteps * 6];
        int stride = uSteps + 1;
        int i = 0;

        for (int v = 0; v < vSteps; v++)
        {
            for (int u = 0; u < uSteps; u++)
            {
                int i0 = v * stride + u;
                int i1 = i0 + 1;
                int i2 = i0 + stride;
                int i3 = i2 + 1;

                indices[i++] = i0;
                indices[i++] = i2;
                indices[i++] = i1;

                indices[i++] = i1;
                indices[i++] = i2;
                indices[i++] = i3;
            }
        }

        return indices;
    }

    private static Vec2[] BuildTextureCoordinates(int uSteps, int vSteps)
    {
        Vec2[] coordinates = new Vec2[(uSteps + 1) * (vSteps + 1)];
        int i = 0;

        for (int v = 0; v <= vSteps; v++)
        {
            for (int u = 0; u <= uSteps; u++)
            {
                coordinates[i++] = new Vec2(u / (double)uSteps, v / (double)vSteps);
            }
        }

        return coordinates;
    }
}
