using CatenoidCore;
using Xunit;

namespace CatenoidCore.Tests;

public class MeshBuildersTests
{
    [Fact]
    public void RingVerticesSitOnTheRequestedRadiiAndPlane()
    {
        MeshData ring = MeshBuilders.BuildRing(innerRadius: 1.0, outerRadius: 1.5, z: -1.75, segments: 32);

        Assert.Equal(66, ring.Positions.Length);
        Assert.Equal(64, ring.TriangleCount);

        for (int i = 0; i < ring.Positions.Length; i += 2)
        {
            Assert.Equal(1.0, ring.Positions[i].RadialDistance, 1e-9);
            Assert.Equal(1.5, ring.Positions[i + 1].RadialDistance, 1e-9);
            Assert.Equal(-1.75, ring.Positions[i].Z, 1e-12);
        }
    }

    [Fact]
    public void ConeInterpolatesBetweenBottomAndTopRims()
    {
        MeshData cone = MeshBuilders.BuildCone(baseRadius: 1.2, topRadius: 0.4, bottomZ: -2.0, topZ: -1.2, segments: 24);

        for (int i = 0; i < cone.Positions.Length; i += 2)
        {
            Assert.Equal(1.2, cone.Positions[i].RadialDistance, 1e-9);
            Assert.Equal(-2.0, cone.Positions[i].Z, 1e-12);
            Assert.Equal(0.4, cone.Positions[i + 1].RadialDistance, 1e-9);
            Assert.Equal(-1.2, cone.Positions[i + 1].Z, 1e-12);
        }
    }

    [Fact]
    public void StripSeamClosesBackOnItself()
    {
        MeshData ring = MeshBuilders.BuildRing(0.5, 1.0, 0, segments: 16);

        Assert.Equal(ring.Positions[0].X, ring.Positions[^2].X, 1e-9);
        Assert.Equal(ring.Positions[0].Y, ring.Positions[^2].Y, 1e-9);
    }

    [Fact]
    public void AllIndicesAreInRange()
    {
        MeshData ring = MeshBuilders.BuildRing(0.2, 0.9, 0.3, segments: 12);

        Assert.All(ring.TriangleIndices, i => Assert.InRange(i, 0, ring.Positions.Length - 1));
    }

    [Fact]
    public void InvalidRadiiAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MeshBuilders.BuildRing(-0.1, 1.0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => MeshBuilders.BuildRing(1.0, 1.0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => MeshBuilders.BuildCone(0, 0.5, 0, 1));
    }

    [Fact]
    public void TooFewSegmentsIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MeshBuilders.BuildRing(0.5, 1.0, 0, segments: 2));
    }

    [Fact]
    public void MeshDataValidatesItsBuffers()
    {
        Assert.Throws<ArgumentException>(() => new MeshData(new Vec3[3], new Vec2[2], new int[3]));
        Assert.Throws<ArgumentException>(() => new MeshData(new Vec3[3], new Vec2[3], new int[4]));
        Assert.Throws<ArgumentNullException>(() => new MeshData(null!, new Vec2[3], new int[3]));
    }
}
