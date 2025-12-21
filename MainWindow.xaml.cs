 using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using NAudio.Wave;

namespace CatenoidDemo
{
    public partial class MainWindow : Window
    {
        // Transforms
        private readonly Transform3DGroup _transformGroup = new Transform3DGroup();
        private readonly RotateTransform3D _rotateX =
            new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0));
        private readonly RotateTransform3D _rotateY =
            new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0));
        private readonly TranslateTransform3D _translate = new TranslateTransform3D(0, 0, 0);
        private readonly ScaleTransform3D _scale = new ScaleTransform3D(1, 1, 1);

        // Animation
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private double _angleX = 0;
        private double _angleY = 0;
        private double _morphPhase = 0;
        private double _movePhase = 0;
        private double _colorPhase = 0;

        // Audio
        private WasapiLoopbackCapture? _capture;
        private float _audioLevel = 0f;
        private double _smoothBeat = 0;

        public MainWindow()
        {
            InitializeComponent();

            BuildSurface(0);
            InitAudio();
            StartAnimation();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_capture != null)
            {
                _capture.StopRecording();
                _capture.Dispose();
                _capture = null;
            }
        }

        private void InitAudio()
        {
            try
            {
                _capture = new WasapiLoopbackCapture();
                _capture.DataAvailable += (s, a) =>
                {
                    float sum = 0;
                    int samples = a.BytesRecorded / 4;
                    if (samples <= 0) return;

                    for (int i = 0; i < a.BytesRecorded; i += 4)
                    {
                        float sample = BitConverter.ToSingle(a.Buffer, i);
                        sum += sample * sample;
                    }

                    float rms = (float)Math.Sqrt(sum / samples);
                    _audioLevel = rms;
                };

                _capture.StartRecording();
            }
            catch
            {
                _audioLevel = 0f;
            }
        }

        private void BuildSurface(double t)
        {
            int U = 72, V = 56;
            double a = 0.6, height = 2.4;

            MeshGeometry3D mesh = new MeshGeometry3D();

            for (int v = 0; v <= V; v++)
            {
                double z = height * (v / (double)V - 0.5);
                for (int u = 0; u <= U; u++)
                {
                    double theta = 2.0 * Math.PI * u / U;
                    Point3D p = MorphPoint(theta, z, a, t);
                    mesh.Positions.Add(p);
                    mesh.TextureCoordinates.Add(new Point(u / (double)U, v / (double)V));
                }
            }

            for (int v = 0; v < V; v++)
            {
                for (int u = 0; u < U; u++)
                {
                    int i0 = v * (U + 1) + u;
                    int i1 = i0 + 1;
                    int i2 = i0 + (U + 1);
                    int i3 = i2 + 1;

                    mesh.TriangleIndices.Add(i0);
                    mesh.TriangleIndices.Add(i2);
                    mesh.TriangleIndices.Add(i1);

                    mesh.TriangleIndices.Add(i1);
                    mesh.TriangleIndices.Add(i2);
                    mesh.TriangleIndices.Add(i3);
                }
            }

            // Original rainbow
            LinearGradientBrush rainbow = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            rainbow.GradientStops.Add(new GradientStop(Colors.Blue, 0.0));
            rainbow.GradientStops.Add(new GradientStop(Colors.Cyan, 0.2));
            rainbow.GradientStops.Add(new GradientStop(Colors.Green, 0.4));
            rainbow.GradientStops.Add(new GradientStop(Colors.Yellow, 0.6));
            rainbow.GradientStops.Add(new GradientStop(Colors.Orange, 0.8));
            rainbow.GradientStops.Add(new GradientStop(Colors.Red, 1.0));

            // ✨ Subtle color drift
            rainbow.GradientStops[0].Color = ShiftHue(rainbow.GradientStops[0].Color, _colorPhase * 0.2);
            rainbow.GradientStops[5].Color = ShiftHue(rainbow.GradientStops[5].Color, _colorPhase * 0.1);

            DiffuseMaterial diffuse = new DiffuseMaterial(rainbow);

            // ✨ Glow (emissive)
            byte glowIntensity = (byte)(60 + 150 * _smoothBeat);
            EmissiveMaterial glow = new EmissiveMaterial(
                new SolidColorBrush(Color.FromArgb(glowIntensity, 255, 255, 255)));

            // ✨ Enhanced specular
            SpecularMaterial specular = new SpecularMaterial(
                new SolidColorBrush(Color.FromRgb(255, 255, 255)),
                40 + 60 * _smoothBeat);

            MaterialGroup mg = new MaterialGroup();
            mg.Children.Add(diffuse);
            mg.Children.Add(glow);
            mg.Children.Add(specular);

            GeometryModel3D model = new GeometryModel3D(mesh, mg)
            {
                BackMaterial = mg
            };

            _transformGroup.Children.Clear();
            _transformGroup.Children.Add(_rotateX);
            _transformGroup.Children.Add(_rotateY);
            _transformGroup.Children.Add(_translate);
            _transformGroup.Children.Add(_scale);

            model.Transform = _transformGroup;
            ModelRoot.Content = model;
        }

        private Point3D MorphPoint(double theta, double z, double a, double t)
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

            return new Point3D(x, y, zz);
        }

        private void StartAnimation()
        {
            _timer.Interval = TimeSpan.FromMilliseconds(30);
            _timer.Tick += Animate;
            _timer.Start();
        }

        private void Animate(object? sender, EventArgs e)
        {
            double beatRaw = Math.Min(_audioLevel * 10.0, 1.0);
            _smoothBeat = 0.8 * _smoothBeat + 0.2 * beatRaw;

            _morphPhase += 0.02;
            _movePhase += 0.03;
            _colorPhase += 0.002;

            double t = (Math.Sin(_morphPhase) + 1) / 2.0;
            t += _smoothBeat * 0.12;
            t = Clamp(t, 0.0, 1.0);

            BuildSurface(t);

            _translate.OffsetY = 0.5 * Math.Sin(_movePhase) + _smoothBeat * 0.25;

            double baseSpeedX = 0.5 + 2.0 * Math.Abs(Math.Sin(_morphPhase * 0.5));
            double baseSpeedY = 1.0 + 3.0 * Math.Abs(Math.Cos(_morphPhase * 0.3));

            double beatFactorX = 1.0 + _smoothBeat * 0.4;
            double beatFactorY = 1.0 + _smoothBeat * 0.6;

            _angleX += baseSpeedX * beatFactorX;
            _angleY += baseSpeedY * beatFactorY;

            if (Math.Abs(Math.Sin(_morphPhase)) > 0.95)
                _angleY += 15 * (1.0 + _smoothBeat * 0.5);

            ((AxisAngleRotation3D)_rotateX.Rotation).Angle = _angleX % 360;
            ((AxisAngleRotation3D)_rotateY.Rotation).Angle = _angleY % 360;

            double scaleFactor =
                1.0 +
                0.05 * Math.Sin(_morphPhase * 2.0) +
                0.15 * _smoothBeat;

            _scale.ScaleX = _scale.ScaleY = _scale.ScaleZ = scaleFactor;
        }

        private static double Clamp(double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private Color ShiftHue(Color c, double shift)
        {
            ColorToHSV(c, out double h, out double s, out double v);
            h = (h + shift) % 1.0;
            return ColorFromHSV(h, s, v);
        }

        private static void ColorToHSV(Color c, out double h, out double s, out double v)
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

        private static Color ColorFromHSV(double h, double s, double v)
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

            return Color.FromRgb(
                (byte)(r * 255),
                (byte)(g * 255),
                (byte)(b * 255));
        }
    }
}