using CatenoidCore;
using Xunit;

namespace CatenoidCore.Tests;

public class CatenoidMathTests
{
    private const double Tolerance = 1e-9;

    [Theory]
    [InlineData(0.6)]
    [InlineData(1.0)]
    [InlineData(2.5)]
    public void CatenoidNeckRadiusIsSmallestAtTheWaist(double a)
    {
        double neck = CatenoidMath.CatenoidRadius(0, a);

        Assert.Equal(a, neck, Tolerance);
        Assert.True(CatenoidMath.CatenoidRadius(0.5, a) > neck);
        Assert.True(CatenoidMath.CatenoidRadius(-0.5, a) > neck);
    }

    [Fact]
    public void CatenoidIsSymmetricAboutTheWaist()
    {
        Assert.Equal(
            CatenoidMath.CatenoidRadius(1.3, 0.6),
            CatenoidMath.CatenoidRadius(-1.3, 0.6),
            Tolerance);
    }

    [Fact]
    public void CatenoidRadiusRejectsNonPositiveNeck()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CatenoidMath.CatenoidRadius(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CatenoidMath.CatenoidRadius(0, -1));
    }

    [Fact]
    public void MorphAtZeroMatchesTheCatenoid()
    {
        Vec3 morphed = CatenoidMath.MorphPoint(0.9, 0.7, 0.6, morph: 0);
        Vec3 expected = CatenoidMath.CatenoidPoint(0.9, 0.7, 0.6);

        AssertClose(expected, morphed);
    }

    [Fact]
    public void MorphAtOneMatchesTheHelicoid()
    {
        Vec3 morphed = CatenoidMath.MorphPoint(0.9, 0.7, 0.6, morph: 1);
        Vec3 expected = CatenoidMath.HelicoidPoint(0.9, 0.7, 0.6);

        AssertClose(expected, morphed);
    }

    [Fact]
    public void MorphIsTheMidpointAtOneHalf()
    {
        Vec3 catenoid = CatenoidMath.CatenoidPoint(1.2, -0.4, 0.6);
        Vec3 helicoid = CatenoidMath.HelicoidPoint(1.2, -0.4, 0.6);
        Vec3 blended = CatenoidMath.MorphPoint(1.2, -0.4, 0.6, morph: 0.5);

        AssertClose((catenoid + helicoid) * 0.5, blended);
    }

    [Theory]
    [InlineData(-3.0)]
    [InlineData(7.5)]
    public void MorphAmountIsClamped(double morph)
    {
        Vec3 clamped = CatenoidMath.MorphPoint(0.4, 0.2, 0.6, morph);
        Vec3 expected = CatenoidMath.MorphPoint(0.4, 0.2, 0.6, Math.Clamp(morph, 0, 1));

        AssertClose(expected, clamped);
    }

    [Fact]
    public void RippleDisplacesRadiallyOnly()
    {
        Vec3 plain = CatenoidMath.MorphPoint(0.8, 0.5, 0.6, 0.3);
        Vec3 rippled = CatenoidMath.MorphPoint(0.8, 0.5, 0.6, 0.3, rippleAmplitude: 0.1, ripplePhase: 1.4);

        Assert.Equal(plain.Z, rippled.Z, Tolerance);
        Assert.NotEqual(plain.RadialDistance, rippled.RadialDistance, Tolerance);
        // Direction around the axis is preserved: the point only slides along its radius.
        Assert.Equal(Math.Atan2(plain.Y, plain.X), Math.Atan2(rippled.Y, rippled.X), 1e-6);
    }

    [Fact]
    public void ZeroRippleAmplitudeLeavesTheSurfaceUntouched()
    {
        Vec3 plain = CatenoidMath.MorphPoint(0.8, 0.5, 0.6, 0.3);
        Vec3 rippled = CatenoidMath.MorphPoint(0.8, 0.5, 0.6, 0.3, rippleAmplitude: 0, ripplePhase: 9.1);

        AssertClose(plain, rippled);
    }

    [Fact]
    public void RippleFactorStaysWithinUnitRange()
    {
        for (int i = 0; i < 500; i++)
        {
            double theta = i * 0.037;
            double z = -1.5 + i * 0.006;
            double factor = CatenoidMath.RippleFactor(theta, z, phase: i * 0.11);

            Assert.InRange(factor, -1.0, 1.0);
        }
    }

    [Fact]
    public void HelicoidRisesLinearlyWithAngle()
    {
        double a = 0.6;
        Vec3 start = CatenoidMath.HelicoidPoint(0, 1.0, a);
        Vec3 quarter = CatenoidMath.HelicoidPoint(Math.PI / 2, 1.0, a);

        Assert.Equal(0, start.Z, Tolerance);
        Assert.Equal(a * (Math.PI / 2) * CatenoidMath.HelicoidPitch, quarter.Z, Tolerance);
    }

    private static void AssertClose(Vec3 expected, Vec3 actual)
    {
        Assert.Equal(expected.X, actual.X, Tolerance);
        Assert.Equal(expected.Y, actual.Y, Tolerance);
        Assert.Equal(expected.Z, actual.Z, Tolerance);
    }
}
