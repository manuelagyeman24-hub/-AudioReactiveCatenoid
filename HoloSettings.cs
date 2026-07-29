using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CatenoidDemo
{
    /// <summary>
    /// Live, two-way bindable scene parameters. The settings panel and the keyboard shortcuts
    /// both mutate this object, which keeps the HUD, the panel and the renderer in sync.
    /// </summary>
    public sealed class HoloSettings : INotifyPropertyChanged
    {
        private double _neckRadius = 0.6;
        private double _height = 2.4;
        private int _uSteps = 80;
        private int _vSteps = 60;
        private double _morphSpeed = 0.66;
        private double _manualMorph = 0.5;
        private double _spinSpeed = 0.9;
        private double _rippleAmount = 0.035;
        private double _glowIntensity = 0.55;
        private double _hueDrift = 0.07;
        private double _backgroundIntensity = 1.0;
        private double _audioSensitivity = 10.0;
        private bool _autoSpin = true;
        private bool _morphPaused;
        private bool _wireframeVisible = true;
        private bool _gridVisible = true;
        private bool _panelVisible = true;
        private int _spinAxis;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Catenoid neck (waist) radius.</summary>
        public double NeckRadius
        {
            get => _neckRadius;
            set => Set(ref _neckRadius, Math.Clamp(value, 0.15, 1.5));
        }

        /// <summary>Total height of the surface along its axis.</summary>
        public double Height
        {
            get => _height;
            set => Set(ref _height, Math.Clamp(value, 0.8, 5.0));
        }

        /// <summary>Divisions around the axis.</summary>
        public int USteps
        {
            get => _uSteps;
            set => Set(ref _uSteps, Math.Clamp(value, 12, 240));
        }

        /// <summary>Divisions along the axis.</summary>
        public int VSteps
        {
            get => _vSteps;
            set => Set(ref _vSteps, Math.Clamp(value, 8, 200));
        }

        /// <summary>Catenoid to helicoid morph rate, in radians per second.</summary>
        public double MorphSpeed
        {
            get => _morphSpeed;
            set => Set(ref _morphSpeed, Math.Clamp(value, 0.0, 3.0));
        }

        /// <summary>Morph amount used while the morph is held.</summary>
        public double ManualMorph
        {
            get => _manualMorph;
            set => Set(ref _manualMorph, Math.Clamp(value, 0.0, 1.0));
        }

        public double SpinSpeed
        {
            get => _spinSpeed;
            set => Set(ref _spinSpeed, Math.Clamp(value, 0.0, 6.0));
        }

        /// <summary>Base radial ripple amplitude; audio adds on top of this.</summary>
        public double RippleAmount
        {
            get => _rippleAmount;
            set => Set(ref _rippleAmount, Math.Clamp(value, 0.0, 0.25));
        }

        public double GlowIntensity
        {
            get => _glowIntensity;
            set => Set(ref _glowIntensity, Math.Clamp(value, 0.0, 1.0));
        }

        /// <summary>Hue rotation speed of the wireframe shell, in turns per second.</summary>
        public double HueDrift
        {
            get => _hueDrift;
            set => Set(ref _hueDrift, Math.Clamp(value, 0.0, 1.0));
        }

        public double BackgroundIntensity
        {
            get => _backgroundIntensity;
            set => Set(ref _backgroundIntensity, Math.Clamp(value, 0.0, 1.5));
        }

        /// <summary>Multiplier applied to the captured audio RMS before it drives the scene.</summary>
        public double AudioSensitivity
        {
            get => _audioSensitivity;
            set => Set(ref _audioSensitivity, Math.Clamp(value, 0.0, 40.0));
        }

        public bool AutoSpin
        {
            get => _autoSpin;
            set => Set(ref _autoSpin, value);
        }

        public bool MorphPaused
        {
            get => _morphPaused;
            set => Set(ref _morphPaused, value);
        }

        public bool WireframeVisible
        {
            get => _wireframeVisible;
            set => Set(ref _wireframeVisible, value);
        }

        public bool GridVisible
        {
            get => _gridVisible;
            set => Set(ref _gridVisible, value);
        }

        public bool PanelVisible
        {
            get => _panelVisible;
            set => Set(ref _panelVisible, value);
        }

        /// <summary>0 = Z (axis of revolution), 1 = Y, 2 = X.</summary>
        public int SpinAxis
        {
            get => _spinAxis;
            set => Set(ref _spinAxis, ((value % 3) + 3) % 3);
        }

        public string SpinAxisName => SpinAxis switch { 1 => "Y", 2 => "X", _ => "Z" };

        public void ResetToDefaults()
        {
            NeckRadius = 0.6;
            Height = 2.4;
            USteps = 80;
            VSteps = 60;
            MorphSpeed = 0.66;
            ManualMorph = 0.5;
            SpinSpeed = 0.9;
            RippleAmount = 0.035;
            GlowIntensity = 0.55;
            HueDrift = 0.07;
            BackgroundIntensity = 1.0;
            AudioSensitivity = 10.0;
            AutoSpin = true;
            MorphPaused = false;
            WireframeVisible = true;
            GridVisible = true;
            SpinAxis = 0;
        }

        private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(SpinAxis))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SpinAxisName)));
        }
    }
}
