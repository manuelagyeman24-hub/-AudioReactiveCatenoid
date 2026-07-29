using CatenoidCore;
using Xunit;

namespace CatenoidCore.Tests;

public class SurfaceTessellationTests
{
    [Fact]
    public void TopologyCountsFollowTheGridResolution()
    {
        SurfaceTessellation tessellation = new(24, 16);

        Assert.Equal(25 * 17, tessellation.VertexCount);
        Assert.Equal(25 * 17, tessellation.TextureCoordinates.Length);
        Assert.Equal(24 * 16 * 2, tessellation.TriangleCount);
        Assert.Equal(24 * 16 * 6, tessellation.TriangleIndices.Length);
    }

    [Fact]
    public void EveryTriangleIndexIsInRange()
    {
        SurfaceTessellation tessellation = new(18, 12);

        Assert.All(tessellation.TriangleIndices, i => Assert.InRange(i, 0, tessellation.VertexCount - 1));
    }

    [Fact]
    public void NoTriangleIsDegenerate()
    {
        SurfaceTessellation tessellation = new(20, 14);
        Vec3[] positions = tessellation.CreatePositionBuffer();
        tessellation.FillPositions(positions, SurfaceParameters.Default);

        int[] indices = tessellation.TriangleIndices;
        for (int i = 0; i < indices.Length; i += 3)
        {
            Vec3 a = positions[indices[i]];
            Vec3 b = positions[indices[i + 1]];
            Vec3 c = positions[indices[i + 2]];
            double doubleArea = Vec3.Cross(b - a, c - a).Length;

            Assert.True(doubleArea > 1e-12, $"Triangle {i / 3} is degenerate.");
        }
    }

    [Fact]
    public void TextureCoordinatesSpanTheUnitSquare()
    {
        SurfaceTessellation tessellation = new(8, 6);
        Vec2 first = tessellation.TextureCoordinates[0];
        Vec2 last = tessellation.TextureCoordinates[^1];

        Assert.Equal(0, first.U, 1e-12);
        Assert.Equal(0, first.V, 1e-12);
        Assert.Equal(1, last.U, 1e-12);
        Assert.Equal(1, last.V, 1e-12);
    }

    [Fact]
    public void SurfaceSpansTheRequestedHeightAndSeamCloses()
    {
        SurfaceTessellation tessellation = new(12, 10);
        Vec3[] positions = tessellation.CreatePositionBuffer();
        SurfaceParameters parameters = new(NeckRadius: 0.6, Height: 2.4, Morph: 0);
        tessellation.FillPositions(positions, parameters);

        Assert.Equal(-1.2, positions[0].Z, 1e-12);
        Assert.Equal(1.2, positions[^1].Z, 1e-12);

        // First and last vertex of a row are the same point on the closed surface.
        Vec3 rowStart = positions[0];
        Vec3 rowEnd = positions[tessellation.USteps];
        Assert.Equal(rowStart.X, rowEnd.X, 1e-9);
        Assert.Equal(rowStart.Y, rowEnd.Y, 1e-9);
    }

    [Fact]
    public void FillPositionsReusesTheCallerBuffer()
    {
        SurfaceTessellation tessellation = new(10, 8);
        Vec3[] positions = tessellation.CreatePositionBuffer();

        tessellation.FillPositions(positions, SurfaceParameters.Default);
        Vec3 firstFrame = positions[5];

        tessellation.FillPositions(positions, SurfaceParameters.Default with { Morph = 1 });
        Vec3 secondFrame = positions[5];

        Assert.NotEqual(firstFrame, secondFrame);
        Assert.Equal(tessellation.VertexCount, positions.Length);
    }

    [Fact]
    public void FillPositionsRejectsWrongSizedBuffers()
    {
        SurfaceTessellation tessellation = new(10, 8);

        Assert.Throws<ArgumentException>(() => tessellation.FillPositions(new Vec3[3], SurfaceParameters.Default));
        Assert.Throws<ArgumentNullException>(() => tessellation.FillPositions(null!, SurfaceParameters.Default));
    }

    [Theory]
    [InlineData(2, 10)]
    [InlineData(10, 2)]
    [InlineData(401, 10)]
    public void ResolutionOutsideTheSupportedRangeIsRejected(int uSteps, int vSteps)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SurfaceTessellation(uSteps, vSteps));
    }

    [Theory]
    [InlineData(0, 2.4)]
    [InlineData(0.6, 0)]
    [InlineData(-1, 2.4)]
    public void DegenerateParametersAreRejected(double neckRadius, double height)
    {
        SurfaceTessellation tessellation = new(8, 8);
        Vec3[] positions = tessellation.CreatePositionBuffer();
        SurfaceParameters parameters = new(neckRadius, height, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => tessellation.FillPositions(positions, parameters));
    }
}
