using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CatenoidCore;

namespace CatenoidDemo
{
    /// <summary>Bridges <see cref="CatenoidCore"/> meshes and brushes into WPF 3D primitives.</summary>
    internal static class HoloGeometry
    {
        public static MeshGeometry3D ToMeshGeometry(MeshData data)
        {
            Point3DCollection positions = new(data.Positions.Length);
            PointCollection coordinates = new(data.TextureCoordinates.Length);
            Int32Collection indices = new(data.TriangleIndices.Length);

            foreach (Vec3 p in data.Positions) positions.Add(new Point3D(p.X, p.Y, p.Z));
            foreach (Vec2 c in data.TextureCoordinates) coordinates.Add(new Point(c.U, c.V));
            foreach (int i in data.TriangleIndices) indices.Add(i);

            MeshGeometry3D mesh = new()
            {
                Positions = positions,
                TextureCoordinates = coordinates,
                TriangleIndices = indices
            };

            mesh.Freeze();
            return mesh;
        }

        /// <summary>Tiled scan-line grid used as the hologram surface texture.</summary>
        public static DrawingBrush CreateHoloGridBrush(Color lineColor, double thickness, int tiles)
        {
            GeometryGroup lines = new();
            lines.Children.Add(new LineGeometry(new Point(0, 0), new Point(1, 0)));
            lines.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, 1)));

            GeometryDrawing drawing = new()
            {
                Geometry = lines,
                Pen = new Pen(new SolidColorBrush(lineColor), thickness)
            };

            DrawingBrush brush = new(drawing)
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
            LinearGradientBrush brush = new()
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

        public static Color ShiftHue(Color c, double shift)
        {
            ColorToHsv(c, out double h, out double s, out double v);
            h = (h + shift) % 1.0;
            if (h < 0) h += 1.0;
            return ColorFromHsv(h, s, v);
        }

        private static void ColorToHsv(Color c, out double h, out double s, out double v)
        {
            double r = c.R / 255.0;
            double g = c.G / 255.0;
            double b = c.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            h = 0;
            if (delta != 0)
            {
                if (max == r) h = (g - b) / delta;
                else if (max == g) h = 2 + (b - r) / delta;
                else h = 4 + (r - g) / delta;
                h /= 6;
                if (h < 0) h += 1;
            }

            s = max == 0 ? 0 : delta / max;
            v = max;
        }

        private static Color ColorFromHsv(double h, double s, double v)
        {
            int i = (int)(h * 6);
            double f = h * 6 - i;
            double p = v * (1 - s);
            double q = v * (1 - f * s);
            double t = v * (1 - (1 - f) * s);

            double r = 0, g = 0, b = 0;

            switch (i % 6)
            {
                case 0: r = v; g = t; b = p; break;
                case 1: r = q; g = v; b = p; break;
                case 2: r = p; g = v; b = t; break;
                case 3: r = p; g = q; b = v; break;
                case 4: r = t; g = p; b = v; break;
                case 5: r = v; g = p; b = q; break;
            }

            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }
    }
}
