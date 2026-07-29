namespace CatenoidCore;

/// <summary>An immutable, renderer agnostic triangle mesh.</summary>
public sealed class MeshData
{
    public MeshData(Vec3[] positions, Vec2[] textureCoordinates, int[] triangleIndices)
    {
        ArgumentNullException.ThrowIfNull(positions);
        ArgumentNullException.ThrowIfNull(textureCoordinates);
        ArgumentNullException.ThrowIfNull(triangleIndices);

        if (textureCoordinates.Length != positions.Length)
            throw new ArgumentException("Texture coordinate count must match position count.", nameof(textureCoordinates));
        if (triangleIndices.Length % 3 != 0)
            throw new ArgumentException("Triangle index count must be a multiple of three.", nameof(triangleIndices));

        Positions = positions;
        TextureCoordinates = textureCoordinates;
        TriangleIndices = triangleIndices;
    }

    public Vec3[] Positions { get; }

    public Vec2[] TextureCoordinates { get; }

    public int[] TriangleIndices { get; }

    public int TriangleCount => TriangleIndices.Length / 3;
}

/// <summary>Builders for the static props of the holographic scene.</summary>
public static class MeshBuilders
{
    /// <summary>Flat annulus in the XY plane, used for the projector plate and its rings.</summary>
    public static MeshData BuildRing(double innerRadius, double outerRadius, double z, int segments = 96)
    {
        if (innerRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(innerRadius), innerRadius, "Inner radius cannot be negative.");
        if (outerRadius <= innerRadius)
            throw new ArgumentOutOfRangeException(nameof(outerRadius), outerRadius, "Outer radius must exceed the inner radius.");

        return BuildStrip(segments, i =>
        {
            double theta = 2.0 * Math.PI * i / segments;
            double c = Math.Cos(theta);
            double s = Math.Sin(theta);
            return (new Vec3(innerRadius * c, innerRadius * s, z), new Vec3(outerRadius * c, outerRadius * s, z));
        });
    }

    /// <summary>Open truncated cone: the projector beam between the plate and the model.</summary>
    public static MeshData BuildCone(double baseRadius, double topRadius, double bottomZ, double topZ, int segments = 96)
    {
        if (baseRadius <= 0 || topRadius < 0)
            throw new ArgumentOutOfRangeException(nameof(baseRadius), baseRadius, "Radii must be positive.");

        return BuildStrip(segments, i =>
        {
            double theta = 2.0 * Math.PI * i / segments;
            double c = Math.Cos(theta);
            double s = Math.Sin(theta);
            return (new Vec3(baseRadius * c, baseRadius * s, bottomZ), new Vec3(topRadius * c, topRadius * s, topZ));
        });
    }

    /// <summary>Builds a closed quad strip from paired inner/outer (or bottom/top) rims.</summary>
    private static MeshData BuildStrip(int segments, Func<int, (Vec3 First, Vec3 Second)> rim)
    {
        if (segments < 3)
            throw new ArgumentOutOfRangeException(nameof(segments), segments, "At least three segments are required.");

        int count = segments + 1;
        Vec3[] positions = new Vec3[count * 2];
        Vec2[] coordinates = new Vec2[count * 2];
        int[] indices = new int[segments * 6];

        for (int i = 0; i < count; i++)
        {
            (Vec3 first, Vec3 second) = rim(i);
            positions[i * 2] = first;
            positions[i * 2 + 1] = second;
            coordinates[i * 2] = new Vec2(i / (double)segments, 0);
            coordinates[i * 2 + 1] = new Vec2(i / (double)segments, 1);
        }

        int k = 0;
        for (int i = 0; i < segments; i++)
        {
            int i0 = i * 2;
            indices[k++] = i0;
            indices[k++] = i0 + 1;
            indices[k++] = i0 + 2;

            indices[k++] = i0 + 2;
            indices[k++] = i0 + 1;
            indices[k++] = i0 + 3;
        }

        return new MeshData(positions, coordinates, indices);
    }
}
