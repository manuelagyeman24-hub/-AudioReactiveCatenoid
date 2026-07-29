using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace CatenoidDemo
{
    /// <summary>
    /// Mesh and brush factories for the holographic scene.
    /// </summary>
    internal static class HoloGeometry
    {
        /// <summary>
        /// Catenoid &lt;-&gt; helicoid morph surface. <paramref name="t"/> is the morph amount (0 = catenoid, 1 = helicoid).
        /// <paramref name="ripple"/> displaces the surface radially for an audio reactive shimmer.
        /// </summary>
        public static MeshGeometry3D BuildMorphSurface(int uSteps, int vSteps, double a, double height, double t, double ripple, double phase)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int v = 0; v <= vSteps; v++)
            {
                double z = height * (v / (double)vSteps - 0.5);
                for (int u = 0; u <= uSteps; u++)
                {
                    double theta = 2.0 * Math.PI * u / uSteps;
                    mesh.Positions.Add(MorphPoint(theta, z, a, t, ripple, phase));
                    mesh.TextureCoordinates.Add(new Point(u / (double)uSteps, v / (double)vSteps));
                }
            }

            for (int v = 0; v < vSteps; v++)
            {
                for (int u = 0; u < uSteps; u++)
                {
                    int i0 = v * (uSteps + 1) + u;
                    int i1 = i0 + 1;
                    int i2 = i0 + (uSteps + 1);
                    int i3 = i2 + 1;

                    mesh.TriangleIndices.Add(i0);
                    mesh.TriangleIndices.Add(i2);
                    mesh.TriangleIndices.Add(i1);

                    mesh.TriangleIndices.Add(i1);
                    mesh.TriangleIndices.Add(i2);
                    mesh.TriangleIndices.Add(i3);
                }
            }

            mesh.Freeze();
            return mesh;
        }

        public static Point3D MorphPoint(double theta, double z, double a, double t, double ripple, double phase)
        {
            double rc = a * Math.Cosh(z / a);
            double xc = rc * Math.Cos(theta);
            double yc = rc * Math.Sin(theta);

            double xh = z * Math.Cos(theta);
            double yh = z * Math.Sin(theta);
            double zh = a * theta * 0.2;

            double x = (1 - t) * xc + t * xh;
            double y = (1 - t) * yc + t * yh;
            double zz = (1 - t) * z + t * zh;

            if (ripple != 0)
            {
                double wave = Math.Sin(6.0 * theta + phase * 2.0) * Math.Cos(3.0 * z - phase);
                double scale = 1.0 + ripple * wave;
                x *= scale;
                y *= scale;
            }

            return new Point3D(x, y, zz);
        }

        /// <summary>Flat glowing ring (annulus) used for the hologram projector base.</summary>
        public static MeshGeometry3D BuildRing(double innerRadius, double outerRadius, double z, int segments = 96)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int i = 0; i <= segments; i++)
            {
                double theta = 2.0 * Math.PI * i / segments;
                double c = Math.Cos(theta);
                double s = Math.Sin(theta);

                mesh.Positions.Add(new Point3D(innerRadius * c, innerRadius * s, z));
                mesh.Positions.Add(new Point3D(outerRadius * c, outerRadius * s, z));
                mesh.TextureCoordinates.Add(new Point(i / (double)segments, 0));
                mesh.TextureCoordinates.Add(new Point(i / (double)segments, 1));
            }

            for (int i = 0; i < segments; i++)
            {
                int i0 = i * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;

                mesh.TriangleIndices.Add(i0);
                mesh.TriangleIndices.Add(i1);
                mesh.TriangleIndices.Add(i2);

                mesh.TriangleIndices.Add(i2);
                mesh.TriangleIndices.Add(i1);
                mesh.TriangleIndices.Add(i3);
            }

            mesh.Freeze();
            return mesh;
        }

        /// <summary>Open projector cone rising from the base plate towards the model.</summary>
        public static MeshGeometry3D BuildProjectorCone(double baseRadius, double topRadius, double bottomZ, double topZ, int segments = 96)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int i = 0; i <= segments; i++)
            {
                double theta = 2.0 * Math.PI * i / segments;
                double c = Math.Cos(theta);
                double s = Math.Sin(theta);

                mesh.Positions.Add(new Point3D(baseRadius * c, baseRadius * s, bottomZ));
                mesh.Positions.Add(new Point3D(topRadius * c, topRadius * s, topZ));
                mesh.TextureCoordinates.Add(new Point(i / (double)segments, 1));
                mesh.TextureCoordinates.Add(new Point(i / (double)segments, 0));
            }

            for (int i = 0; i < segments; i++)
            {
                int i0 = i * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;

                mesh.TriangleIndices.Add(i0);
                mesh.TriangleIndices.Add(i1);
                mesh.TriangleIndices.Add(i2);

                mesh.TriangleIndices.Add(i2);
                mesh.TriangleIndices.Add(i1);
                mesh.TriangleIndices.Add(i3);
            }

            mesh.Freeze();
            return mesh;
        }

        /// <summary>Tiled scan-line grid used as the hologram surface texture.</summary>
        public static DrawingBrush CreateHoloGridBrush(Color lineColor, double thickness, int tiles)
        {
            GeometryGroup lines = new GeometryGroup();
            lines.Children.Add(new LineGeometry(new Point(0, 0), new Point(1, 0)));
            lines.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, 1)));

            GeometryDrawing drawing = new GeometryDrawing
            {
                Geometry = lines,
                Pen = new Pen(new SolidColorBrush(lineColor), thickness)
            };

            DrawingBrush brush = new DrawingBrush(drawing)
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 1.0 / tiles, 1.0 / tiles),
                ViewportUnits = BrushMappingMode.RelativeToBoundingBox,
                Viewbox = new Rect(0, 0, 1, 1),
                ViewboxUnits = BrushMappingMode.RelativeToBoundingBox
            };

            brush.Freeze();
            return brush;
        }

        /// <summary>Vertical cyan to magenta gradient with a bright interference band in the middle.</summary>
        public static LinearGradientBrush CreateHoloGradient(byte alpha)
        {
            LinearGradientBrush brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };

            brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, 0, 240, 255), 0.0));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, 80, 160, 255), 0.35));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, 200, 255, 255), 0.5));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, 170, 90, 255), 0.7));
            brush.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, 255, 80, 200), 1.0));

            return brush;
        }
    }
}
