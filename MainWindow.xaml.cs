using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Windows.Threading;
using NAudio.Wave;

namespace CatenoidDemo
{
    public partial class MainWindow : Window
    {
        private const int USteps = 80;
        private const int VSteps = 60;
        private const double SurfaceA = 0.6;
        private const double SurfaceHeight = 2.4;

        private const double DefaultPitch = -72;
        private const double DefaultYaw = -24;
        private const double DefaultDistance = 8;
        private const double MinDistance = 3.5;
        private const double MaxDistance = 22;

        // Hologram transforms
        private readonly Transform3DGroup _modelTransform = new Transform3DGroup();
        private readonly Transform3DGroup _baseTransform = new Transform3DGroup();
        private readonly AxisAngleRotation3D _spinRotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
        private readonly AxisAngleRotation3D _basePlateRotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);
        private readonly AxisAngleRotation3D _pitchRotation = new AxisAngleRotation3D(new Vector3D(1, 0, 0), DefaultPitch);
        private readonly AxisAngleRotation3D _yawRotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), DefaultYaw);
        private readonly TranslateTransform3D _floatTranslate = new TranslateTransform3D(0, 0, 0);
        private readonly TranslateTransform3D _panTranslate = new TranslateTransform3D(0, 0, 0);
        private readonly ScaleTransform3D _pulseScale = new ScaleTransform3D(1, 1, 1);
        private readonly ScaleTransform3D _wireOffsetScale = new ScaleTransform3D(1.012, 1.012, 1.012);

        // Cached materials
        private readonly SolidColorBrush _glowBrush = new SolidColorBrush(Color.FromArgb(70, 200, 255, 255));
        private readonly SolidColorBrush _wireGlowBrush = new SolidColorBrush(Color.FromArgb(120, 120, 240, 255));
        private MaterialGroup _holoMaterial = new MaterialGroup();
        private MaterialGroup _wireMaterial = new MaterialGroup();
        private GeometryModel3D? _holoModel;
        private GeometryModel3D? _wireModel;

        // Background layers
        private readonly TranslateTransform _gridDrift = new TranslateTransform();
        private readonly TranslateTransform _scanlineDrift = new TranslateTransform();
        private readonly TranslateTransform _sweepDrift = new TranslateTransform();
        private readonly List<Star> _stars = new List<Star>();
        private readonly Random _random = new Random(20260116);

        // Animation state
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private double _morphPhase;
        private double _floatPhase;
        private double _colorPhase;
        private double _spinAngle;
        private double _spinSpeed = 0.9;
        private double _distance = DefaultDistance;
        private int _spinAxis;                 // 0 = Z, 1 = Y, 2 = X
        private bool _autoSpin = true;
        private bool _morphPaused;
        private bool _wireframeVisible = true;
        private long _lastFrameTicks;
        private double _fps;
        private int _readoutCountdown;

        // Interaction state
        private bool _rotating;
        private bool _panning;
        private Point _lastMousePosition;

        // Audio
        private WasapiLoopbackCapture? _capture;
        private float _audioLevel;
        private double _smoothBeat;
        private bool _audioAvailable;

        private struct Star
        {
            public Ellipse Dot;
            public double X;
            public double Y;
            public double Speed;
            public double Phase;
            public double BaseOpacity;
        }

        public MainWindow()
        {
            InitializeComponent();

            BuildMaterials();
            BuildProjectorBase();
            BuildSurface(0);
            BuildBackgroundLayers();
            ApplyCamera();
            InitAudio();
            StartAnimation();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
            BuildStarField();
            SizeChanged += (_, _) => BuildStarField();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _timer.Stop();

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
                _audioAvailable = true;
            }
            catch
            {
                _audioLevel = 0f;
                _audioAvailable = false;
            }
        }

        // ---------------------------------------------------------------- 3D scene

        private void BuildMaterials()
        {
            _holoMaterial = new MaterialGroup();
            _holoMaterial.Children.Add(new DiffuseMaterial(HoloGeometry.CreateHoloGradient(120)));
            _holoMaterial.Children.Add(new EmissiveMaterial(HoloGeometry.CreateHoloGridBrush(Color.FromArgb(150, 0, 235, 255), 0.06, 18)));
            _holoMaterial.Children.Add(new EmissiveMaterial(_glowBrush));
            _holoMaterial.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromRgb(220, 245, 255)), 60));

            _wireMaterial = new MaterialGroup();
            _wireMaterial.Children.Add(new EmissiveMaterial(HoloGeometry.CreateHoloGridBrush(Color.FromArgb(190, 170, 120, 255), 0.05, 34)));
            _wireMaterial.Children.Add(new EmissiveMaterial(_wireGlowBrush));

            _modelTransform.Children.Clear();
            _modelTransform.Children.Add(new RotateTransform3D(_spinRotation));
            _modelTransform.Children.Add(_pulseScale);
            _modelTransform.Children.Add(_floatTranslate);
            _modelTransform.Children.Add(new RotateTransform3D(_pitchRotation));
            _modelTransform.Children.Add(new RotateTransform3D(_yawRotation));
            _modelTransform.Children.Add(_panTranslate);

            _baseTransform.Children.Clear();
            _baseTransform.Children.Add(new RotateTransform3D(_basePlateRotation));
            _baseTransform.Children.Add(new RotateTransform3D(_pitchRotation));
            _baseTransform.Children.Add(new RotateTransform3D(_yawRotation));
            _baseTransform.Children.Add(_panTranslate);
        }

        /// <summary>Glowing projector plate and light cone that make the model read as a hologram.</summary>
        private void BuildProjectorBase()
        {
            double plateZ = -(SurfaceHeight / 2.0) - 0.55;
            Model3DGroup group = new Model3DGroup();

            group.Children.Add(new GeometryModel3D(
                HoloGeometry.BuildRing(0.05, 2.6, plateZ),
                new EmissiveMaterial(new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop(Color.FromArgb(90, 0, 200, 255), 0.0),
                        new GradientStop(Color.FromArgb(40, 90, 60, 255), 0.6),
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0)
                    }
                })
            )
            { BackMaterial = new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(30, 0, 180, 255))) });

            foreach (double radius in new[] { 1.35, 1.9, 2.45 })
            {
                EmissiveMaterial ringMaterial = new EmissiveMaterial(
                    new SolidColorBrush(Color.FromArgb(150, 0, 230, 255)));

                group.Children.Add(new GeometryModel3D(
                    HoloGeometry.BuildRing(radius, radius + 0.035, plateZ + 0.002),
                    ringMaterial)
                { BackMaterial = ringMaterial });
            }

            EmissiveMaterial coneMaterial = new EmissiveMaterial(new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(45, 0, 220, 255), 0.0),
                    new GradientStop(Color.FromArgb(8, 120, 90, 255), 1.0)
                }
            });

            group.Children.Add(new GeometryModel3D(
                HoloGeometry.BuildProjectorCone(1.15, 0.35, plateZ + 0.01, -SurfaceHeight / 2.0),
                coneMaterial)
            { BackMaterial = coneMaterial });

            group.Transform = _baseTransform;
            BaseRoot.Content = group;
        }

        private void BuildSurface(double t)
        {
            double ripple = 0.035 + 0.09 * _smoothBeat;
            MeshGeometry3D mesh = HoloGeometry.BuildMorphSurface(
                USteps, VSteps, SurfaceA, SurfaceHeight, t, ripple, _morphPhase);

            if (_holoModel == null)
            {
                _holoModel = new GeometryModel3D(mesh, _holoMaterial)
                {
                    BackMaterial = _holoMaterial,
                    Transform = _modelTransform
                };
                ModelRoot.Content = _holoModel;
            }
            else
            {
                _holoModel.Geometry = mesh;
            }

            if (_wireModel == null)
            {
                Transform3DGroup wireTransform = new Transform3DGroup();
                wireTransform.Children.Add(_wireOffsetScale);
                wireTransform.Children.Add(_modelTransform);

                _wireModel = new GeometryModel3D(mesh, _wireMaterial)
                {
                    BackMaterial = _wireMaterial,
                    Transform = wireTransform
                };
                WireRoot.Content = _wireModel;
            }
            else
            {
                _wireModel.Geometry = mesh;
            }
        }

        private void ApplyCamera()
        {
            Camera.Position = new Point3D(0, 0, _distance);
            Camera.LookDirection = new Vector3D(0, 0, -_distance);
        }

        // ---------------------------------------------------------- background layers

        private void BuildBackgroundLayers()
        {
            DrawingBrush gridBrush = new DrawingBrush(new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 56, 56)),
                Pen = new Pen(new SolidColorBrush(Color.FromArgb(120, 60, 200, 255)), 0.7)
            })
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 56, 56),
                ViewportUnits = BrushMappingMode.Absolute,
                Transform = _gridDrift
            };
            GridLayer.Fill = gridBrush;
            GridLayer.OpacityMask = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.0),
                    new GradientStop(Color.FromArgb(80, 0, 0, 0), 0.35),
                    new GradientStop(Color.FromArgb(255, 0, 0, 0), 1.0)
                }
            };

            DrawingBrush scanlineBrush = new DrawingBrush(new GeometryDrawing
            {
                Geometry = new RectangleGeometry(new Rect(0, 0, 4, 1.6)),
                Brush = new SolidColorBrush(Color.FromArgb(255, 190, 240, 255))
            })
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 4, 4),
                ViewportUnits = BrushMappingMode.Absolute,
                Transform = _scanlineDrift
            };
            ScanlineLayer.Fill = scanlineBrush;

            SweepLayer.Fill = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromArgb(0, 120, 240, 255), 0.0),
                    new GradientStop(Color.FromArgb(90, 190, 250, 255), 0.5),
                    new GradientStop(Color.FromArgb(0, 120, 240, 255), 1.0)
                }
            };
            SweepLayer.Height = 180;
            SweepLayer.VerticalAlignment = VerticalAlignment.Top;
            SweepLayer.RenderTransform = _sweepDrift;
        }

        private void BuildStarField()
        {
            StarLayer.Children.Clear();
            _stars.Clear();

            double width = Math.Max(ActualWidth, 320);
            double height = Math.Max(ActualHeight, 240);

            for (int i = 0; i < 160; i++)
            {
                double size = 0.7 + _random.NextDouble() * 2.1;
                double opacity = 0.15 + _random.NextDouble() * 0.6;

                Ellipse dot = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = new SolidColorBrush(_random.NextDouble() < 0.25
                        ? Color.FromRgb(255, 180, 240)
                        : Color.FromRgb(190, 235, 255)),
                    Opacity = opacity
                };

                Star star = new Star
                {
                    Dot = dot,
                    X = _random.NextDouble() * width,
                    Y = _random.NextDouble() * height,
                    Speed = 4 + _random.NextDouble() * 22,
                    Phase = _random.NextDouble() * Math.PI * 2,
                    BaseOpacity = opacity
                };

                Canvas.SetLeft(dot, star.X);
                Canvas.SetTop(dot, star.Y);
                StarLayer.Children.Add(dot);
                _stars.Add(star);
            }
        }

        private void AnimateBackground(double dt, double time)
        {
            NebulaARotate.Angle = time * 1.6;
            NebulaBRotate.Angle = -time * 1.1;

            double breathA = 1.0 + 0.06 * Math.Sin(time * 0.5) + 0.05 * _smoothBeat;
            double breathB = 1.0 + 0.05 * Math.Cos(time * 0.35) + 0.04 * _smoothBeat;
            NebulaAScale.ScaleX = NebulaAScale.ScaleY = breathA;
            NebulaBScale.ScaleX = NebulaBScale.ScaleY = breathB;
            NebulaA.Opacity = 0.45 + 0.2 * _smoothBeat;
            NebulaB.Opacity = 0.35 + 0.18 * Math.Abs(Math.Sin(time * 0.4));

            _gridDrift.X = (_gridDrift.X + dt * 9.0) % 56.0;
            _gridDrift.Y = (_gridDrift.Y + dt * 16.0) % 56.0;
            GridLayer.Opacity = GridLayer.Visibility == Visibility.Visible
                ? 0.16 + 0.14 * _smoothBeat
                : 0.0;

            _scanlineDrift.Y = (_scanlineDrift.Y + dt * 26.0) % 4.0;
            ScanlineLayer.Opacity = 0.08 + 0.05 * Math.Abs(Math.Sin(time * 3.0));

            double sweepCycle = (time * 0.22) % 1.0;
            _sweepDrift.Y = sweepCycle * (ActualHeight + 180) - 180;
            SweepLayer.Opacity = 0.10 + 0.10 * Math.Sin(sweepCycle * Math.PI);

            // Hologram flicker
            Viewport.Opacity = 0.93 + 0.07 * Math.Abs(Math.Sin(time * 7.3)) - (_random.NextDouble() < 0.02 ? 0.12 : 0.0);

            double height = Math.Max(ActualHeight, 240);
            for (int i = 0; i < _stars.Count; i++)
            {
                Star star = _stars[i];
                star.Y -= star.Speed * dt * (1.0 + _smoothBeat);
                if (star.Y < -4) star.Y += height + 8;

                Canvas.SetTop(star.Dot, star.Y);
                star.Dot.Opacity = star.BaseOpacity * (0.55 + 0.45 * Math.Sin(time * 2.0 + star.Phase));
                _stars[i] = star;
            }
        }

        // ------------------------------------------------------------------ animation

        private void StartAnimation()
        {
            _lastFrameTicks = _clock.ElapsedTicks;
            _timer.Interval = TimeSpan.FromMilliseconds(16);
            _timer.Tick += Animate;
            _timer.Start();
        }

        private void Animate(object? sender, EventArgs e)
        {
            long now = _clock.ElapsedTicks;
            double dt = (now - _lastFrameTicks) / (double)Stopwatch.Frequency;
            _lastFrameTicks = now;
            dt = Clamp(dt, 0.001, 0.1);
            double time = _clock.Elapsed.TotalSeconds;
            _fps = _fps <= 0 ? 1.0 / dt : 0.9 * _fps + 0.1 / dt;

            double beatRaw = Math.Min(_audioLevel * 10.0, 1.0);
            _smoothBeat = 0.8 * _smoothBeat + 0.2 * beatRaw;

            if (!_morphPaused) _morphPhase += dt * 0.66;
            _floatPhase += dt * 1.0;
            _colorPhase += dt * 0.07;

            double t = (Math.Sin(_morphPhase) + 1) / 2.0;
            t = Clamp(t + _smoothBeat * 0.12, 0.0, 1.0);
            BuildSurface(t);

            if (_autoSpin)
            {
                _spinAngle += dt * 60.0 * _spinSpeed * (1.0 + _smoothBeat * 0.8);
                _spinRotation.Angle = _spinAngle % 360;
            }

            _basePlateRotation.Angle = (time * -14.0) % 360;

            _floatTranslate.OffsetZ = 0.22 * Math.Sin(_floatPhase) + _smoothBeat * 0.12;

            double pulse = 1.0 + 0.03 * Math.Sin(_floatPhase * 2.0) + 0.14 * _smoothBeat;
            _pulseScale.ScaleX = _pulseScale.ScaleY = _pulseScale.ScaleZ = pulse;

            byte glowAlpha = (byte)(55 + 150 * _smoothBeat);
            _glowBrush.Color = Color.FromArgb(glowAlpha, 200, 245, 255);

            Color wireColor = ShiftHue(Color.FromRgb(90, 220, 255), _colorPhase % 1.0);
            _wireGlowBrush.Color = Color.FromArgb((byte)(70 + 90 * _smoothBeat), wireColor.R, wireColor.G, wireColor.B);

            AnimateBackground(dt, time);

            if (--_readoutCountdown <= 0)
            {
                _readoutCountdown = 12;
                UpdateReadout(t);
            }
        }

        private void UpdateReadout(double morph)
        {
            string axis = _spinAxis switch { 1 => "Y", 2 => "X", _ => "Z" };
            Readout.Text =
                $"FPS {_fps,5:0.0}\n" +
                $"SPIN {(_autoSpin ? "ON " : "OFF")}  AXIS {axis}  x{_spinSpeed:0.0}\n" +
                $"MORPH {(_morphPaused ? "HOLD" : "LIVE")} {morph:0.00}\n" +
                $"YAW {Normalize(_yawRotation.Angle),6:0}°  PITCH {Normalize(_pitchRotation.Angle),6:0}°\n" +
                $"ZOOM {_distance:0.0}\n" +
                $"AUDIO {(_audioAvailable ? $"{_smoothBeat:0.00}" : "N/A")}";
        }

        // ---------------------------------------------------------------- interaction

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _rotating = true;
            _lastMousePosition = e.GetPosition(this);
            Mouse.Capture(this);
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _rotating = false;
            ReleaseCaptureIfIdle();
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _panning = true;
            _lastMousePosition = e.GetPosition(this);
            Mouse.Capture(this);
        }

        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _panning = false;
            ReleaseCaptureIfIdle();
        }

        private void ReleaseCaptureIfIdle()
        {
            if (!_rotating && !_panning) Mouse.Capture(null);
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_rotating && !_panning) return;

            Point position = e.GetPosition(this);
            double dx = position.X - _lastMousePosition.X;
            double dy = position.Y - _lastMousePosition.Y;
            _lastMousePosition = position;

            if (_rotating)
            {
                _yawRotation.Angle = Normalize(_yawRotation.Angle + dx * 0.4);
                _pitchRotation.Angle = Normalize(_pitchRotation.Angle - dy * 0.4);
            }
            else
            {
                double scale = _distance * 0.0016;
                _panTranslate.OffsetX += dx * scale;
                _panTranslate.OffsetY -= dy * scale;
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Zoom(-e.Delta / 240.0);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            double step = 0.25;

            switch (e.Key)
            {
                case Key.Space:
                    _autoSpin = !_autoSpin;
                    break;
                case Key.X:
                    _spinAxis = (_spinAxis + 1) % 3;
                    _spinRotation.Axis = _spinAxis switch
                    {
                        1 => new Vector3D(0, 1, 0),
                        2 => new Vector3D(1, 0, 0),
                        _ => new Vector3D(0, 0, 1)
                    };
                    break;
                case Key.OemOpenBrackets:
                    _spinSpeed = Clamp(_spinSpeed - 0.2, 0.0, 6.0);
                    break;
                case Key.OemCloseBrackets:
                    _spinSpeed = Clamp(_spinSpeed + 0.2, 0.0, 6.0);
                    break;
                case Key.M:
                    _morphPaused = !_morphPaused;
                    break;
                case Key.H:
                    _wireframeVisible = !_wireframeVisible;
                    WireRoot.Content = _wireframeVisible ? _wireModel : null;
                    break;
                case Key.G:
                    GridLayer.Visibility = GridLayer.Visibility == Visibility.Visible
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                    break;
                case Key.R:
                    ResetView();
                    break;
                case Key.W:
                case Key.Up:
                    _panTranslate.OffsetY += step;
                    break;
                case Key.S:
                case Key.Down:
                    _panTranslate.OffsetY -= step;
                    break;
                case Key.A:
                case Key.Left:
                    _panTranslate.OffsetX -= step;
                    break;
                case Key.D:
                case Key.Right:
                    _panTranslate.OffsetX += step;
                    break;
                case Key.OemPlus:
                case Key.Add:
                    Zoom(-0.5);
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    Zoom(0.5);
                    break;
                case Key.Q:
                    _yawRotation.Angle = Normalize(_yawRotation.Angle - 5);
                    break;
                case Key.E:
                    _yawRotation.Angle = Normalize(_yawRotation.Angle + 5);
                    break;
            }
        }

        private void Zoom(double amount)
        {
            _distance = Clamp(_distance + amount, MinDistance, MaxDistance);
            ApplyCamera();
        }

        private void ResetView()
        {
            _pitchRotation.Angle = DefaultPitch;
            _yawRotation.Angle = DefaultYaw;
            _panTranslate.OffsetX = 0;
            _panTranslate.OffsetY = 0;
            _spinAngle = 0;
            _spinRotation.Angle = 0;
            _spinAxis = 0;
            _spinRotation.Axis = new Vector3D(0, 0, 1);
            _spinSpeed = 0.9;
            _autoSpin = true;
            _morphPaused = false;
            _distance = DefaultDistance;
            ApplyCamera();
        }

        // -------------------------------------------------------------------- helpers

        private static double Normalize(double angle)
        {
            angle %= 360;
            if (angle > 180) angle -= 360;
            if (angle < -180) angle += 360;
            return angle;
        }

        private static double Clamp(double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private static Color ShiftHue(Color c, double shift)
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

            return Color.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }
    }
}
