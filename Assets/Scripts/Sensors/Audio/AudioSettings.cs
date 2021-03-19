using UnityEngine;

namespace MBaske.Sensors.Audio
{
    /// <summary>
    /// SignalType.Stereo samples left and right channel values sparately.
    /// SignalType.Mono samples the mean values of left and right channels.
    /// </summary>
    public enum SignalType
    {
        Stereo, Mono
    }

    /// <summary>
    /// SampleType.Amplitude samples amplitude values <see cref="AudioListener.GetOutputData"/>.
    /// SampleType.Spectrum samples FFT band values <see cref="AudioListener.GetSpectrumData"/>.
    /// </summary>
    public enum SampleType
    {
        Amplitude, Spectrum
    }

    /// <summary>
    /// Observation shape of the <see cref="AudioSensor"/>.
    /// </summary>
    [System.Serializable]
    public struct SensorObservationShape
    {
        public SignalType SignalType;
        public int SignalChannels => SignalType == SignalType.Stereo ? 2 : 1;

        public int BufferLength
        {
            set { Channels = value * SignalChannels; }
        }

        public int Channels { get; private set; }
        public int Width;
        public int Height;

        public int[] ToArray() => new[] { Height, Width, Channels };
        public override string ToString() => $"Sensor shape: {Height} x {Width} x {Channels}";
    }

    /// <summary>
    /// Stores a couple of <see cref="AudioSensorComponent"/> settings for detecting specific editor changes.
    /// </summary>
    [System.Serializable]
    public struct SettingsWatcher
    {
        public SampleType SampleType;
        public SignalType SignalType;
        public FFTWindow FFTWindow;
        public int FFTResolution;
        public float FFTFloor;
        public float AmpFloor;

        /// <summary>
        /// Stores the current settings.
        /// </summary>
        /// <param name="component">The audio sensor component.</param>
        public void StoreSettings(AudioSensorComponent component)
        {
            SampleType = component.SampleType;
            SignalType = component.SignalType;
            FFTWindow = component.FFTWindow;
            FFTResolution = component.FFTResolution;
            FFTFloor = component.FFTFloor;
            AmpFloor = component.AmpFloor;
        }

        /// <summary>
        /// Checks if the FFT resolution has changed since the last settings update.
        /// </summary>
        /// <param name="component">The audio sensor component.</param>
        /// <returns>true if the FFT resolution has changed.</returns>
        public bool FFTResolutionChanged(AudioSensorComponent component)
        {
            return FFTResolution != component.FFTResolution;
        }

        /// <summary>
        /// Checks if size of the samples array has become invalid.
        /// </summary>
        /// <param name="component">The audio sensor component.</param>
        /// <returns>true if the sample size is invalid.</returns>
        public bool SampleSizeInvalid(AudioSensorComponent component)
        {
            return FFTResolutionChanged(component)
                || SampleType != component.SampleType;
        }

        /// <summary>
        /// Checks if the spectrum normalization data has become invalid.
        /// </summary>
        /// <param name="component">The audio sensor component.</param>
        /// <returns>true if the normalization data is invalid.</returns>
        public bool SpectrumNormalizationInvalid(AudioSensorComponent component)
        {
            return FFTResolutionChanged(component)
                || SignalType != component.SignalType
                || FFTWindow != component.FFTWindow
                || FFTFloor != component.FFTFloor;
        }

        /// <summary>
        /// Checks if the amplitude normalization data has become invalid.
        /// </summary>
        /// <param name="component">The audio sensor component.</param>
        /// <returns>true if the normalization data is invalid.</returns>
        public bool AmplitudeNormalizationInvalid(AudioSensorComponent component)
        {
            return SignalType != component.SignalType
                || AmpFloor != component.AmpFloor;
        }
    }
}