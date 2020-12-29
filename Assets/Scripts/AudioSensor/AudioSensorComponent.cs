using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Linq;
using System;
using AudioSensor.Util;

namespace AudioSensor
{
    /// <summary>
    /// Component that wraps an <see cref="AudioSensor"/>.
    /// The component is responsible for sampling and processing <see cref="AudioListener"/> data
    /// and for writing it to the <see cref="AudioBuffer"/>. The wrapped sensor generates observations 
    /// from the buffer contents.
    /// </summary>
    public class AudioSensorComponent : SensorComponent, IAudioSampler
    {
        #region Editor Settings

        /// <summary>
        /// Name of the generated <see cref="AudioSensor"/>.
        /// </summary>
        public string SensorName
        {
            get { return m_SensorName; }
            set { m_SensorName = value; }
        }
        [HideInInspector, SerializeField]
        private string m_SensorName = "AudioSensor";


        /// <summary>
        /// Length of the buffer in update / sampling steps.
        /// Multiply with update delta time to get length in seconds.
        /// </summary>
        public int BufferLength
        {
            get { return m_BufferLength; }
            set { m_BufferLength = value; }
        }
        [HideInInspector, SerializeField]
        private int m_BufferLength = 1;


        /// <summary>
        /// The compression type to use for the sensor.
        /// </summary>
        public SensorCompressionType CompressionType
        {
            get { return m_CompressionType; }
            set { m_CompressionType = value; }
        }
        [HideInInspector, SerializeField]
        private SensorCompressionType m_CompressionType = SensorCompressionType.None;


        /// <summary>
        /// The signal type (mono or stereo) to use for the sensor.
        /// </summary>
        public SignalType SignalType
        {
            get { return m_SignalType; }
            set { m_SignalType = value; }
        }
        [HideInInspector, SerializeField]
        private SignalType m_SignalType = SignalType.Stereo;


        /// <summary>
        /// The sample type (amplitude or spectrum) to use for the sensor.
        /// </summary>
        public SampleType SampleType
        {
            get { return m_SampleType; }
            set { m_SampleType = value; }
        }
        [HideInInspector, SerializeField]
        private SampleType m_SampleType = SampleType.Spectrum;


        /// <summary>
        /// The <see cref="UnityEngine.FFTWindow"/> for sampling spectrum data.
        /// </summary>
        public FFTWindow FFTWindow
        {
            get { return m_FFTWindow; }
            set { m_FFTWindow = value; }
        }
        [HideInInspector, SerializeField]
        private FFTWindow m_FFTWindow = FFTWindow.Rectangular;


        /// <summary>
        /// The number of FFT bands when sampling spectrum data.
        /// Must be between 2^6 (64) and 2^13 (8192).
        /// This is the number of bands being sampled, NOT the number
        /// of bands being observed. Use <see cref="FFTMinFrequency"/>
        /// and <see cref="FFTMaxFrequency"/> to constrain the observation.
        /// </summary>
        public int FFTResolution
        {
            get { return (int)Mathf.Pow(2, m_FFTBitWidth); }
            set { m_FFTBitWidth = (int)Mathf.Log(value, 2); }
        }
        [HideInInspector, SerializeField]
        private int m_FFTBitWidth = 10;


        /// <summary>
        /// The lowest observed frequency when sampling spectrum data.
        /// Note that this is not an audio filter, but a hard cut with respect to the FFT 
        /// bands included in the observation.
        /// The precision can only be as high as the selected <see cref="FFTResolution"/>,
        /// adjusting frequencies that lie within the same FFT band has no effect.
        /// Leakage from neighbouring bands may depend on the selected <see cref="FFTWindow"/>.
        /// </summary>
        public float FFTMinFrequency
        {
            get { return Mathf.Pow(10, m_FFTMinFreqLog); }
            set { m_FFTMinFreqLog = Mathf.Log(value, 10); }
        }
        [HideInInspector, SerializeField]
        private float m_FFTMinFreqLog = Frequeny.Log20Hz;


        /// <summary>
        /// The highest observed frequency when sampling spectrum data.
        /// Note that this is not an audio filter, but a hard cut with respect to the FFT 
        /// bands included in the observation.
        /// The precision can only be as high as the selected <see cref="FFTResolution"/>,
        /// adjusting frequencies that lie within the same FFT band has no effect.
        /// Leakage from neighbouring bands may depend on the selected <see cref="FFTWindow"/>.
        /// </summary>
        public float FFTMaxFrequency
        {
            get { return Mathf.Pow(10, m_FFTMaxFreqLog); }
            set { m_FFTMaxFreqLog = Mathf.Log(value, 10); }
        }
        [HideInInspector, SerializeField]
        private float m_FFTMaxFreqLog = Frequeny.Log20kHz;


        /// <summary>
        /// The minimum decibel value when scaling FFT band values.
        /// The field is serialized as a logarithmic value because
        /// I found that more intuitive for the editor slider.
        /// </summary>
        public float FFTFloor
        {
            get { return -3 * Mathf.Pow(2, -m_FFTFloorLog); }
            set { m_FFTFloorLog = -Mathf.Log(value / -3, 2); }
        }
        [HideInInspector, SerializeField]
        private float m_FFTFloorLog = -4.321928f; // -60dB


        /// <summary>
        /// The minimum decibel value when scaling amplitudes.
        /// The field is serialized as a logarithmic value because
        /// I found that more intuitive for the editor slider.
        /// </summary>
        public float AmpFloor
        {
            get { return -3 * Mathf.Pow(2, -m_AmpFloorLog); }
            set { m_AmpFloorLog = -Mathf.Log(value / -3, 2); }
        }
        [HideInInspector, SerializeField]
        private float m_AmpFloorLog = -4.321928f; // -60dB


        /// <summary>
        /// Whether to normalize the samples.
        /// Normalization is implemented as upward expansion over the full dynamic range,
        /// based on the measured signal peaks. When sampling spectrum data, each FFT band
        /// is normalized individually.
        /// </summary>
        public bool Normalize
        {
            get { return m_Normalize; }
            set { m_Normalize = value; }
        }
        [HideInInspector, SerializeField]
        private bool m_Normalize;

        #endregion


        /// <summary>
        /// Observation shape of the sensor.
        /// </summary>
        public SensorObservationShape Shape
        {
            get { return m_Shape; }
            set { m_Shape = value; }
        }
        [HideInInspector, SerializeField]
        private SensorObservationShape m_Shape;

        /// <summary>
        /// The wrapped audio sensor.
        /// </summary>
        public AudioSensor Sensor { get; private set; }

        /// <inheritdoc/>
        /// <see cref="IAudioSampler"/>
        public event Action<int, bool> SamplingUpdateEvent;

        /// <summary>
        /// Event is triggered when component settings are updated.
        /// </summary>
        public event Action<AudioSensorComponent> SettingsUpdateEvent;


        /// <inheritdoc/>
        /// <see cref="IAudioSampler"/>
        public bool SamplingEnabled { get; set; }

        /// <summary>
        /// Flag indicating whether any clipping occurred during the latest sample call.
        /// Clipping in this case refers to any normalized sample values being >= 1.
        /// This is intended behaviour while calibrating normalization and is used for 
        /// drawing a red bar in the editor graph, indicating that more signal peaks 
        /// likely need to be measured for fitting normalization to the audio.
        /// </summary>
        public bool IsClipping { get; private set; }

        /// <summary>
        /// Whether the user is calibrating the sensor in play mode.
        /// Calibration should be inactive during training.
        /// </summary>
        public bool IsCalibrating => Application.isPlaying && m_CalibrationButtonActive;

        /// <summary>
        /// Whether to measure signal peaks for fitting normalization.
        /// </summary>
        private bool MeasurePeaks => m_Normalize && IsCalibrating;

        /// <summary>
        /// The number of observed samples per channel.
        /// </summary>
        public int SamplesPerChannel { get; private set; }


        // Stores editor button state.
        [HideInInspector, SerializeField]
        private bool m_CalibrationButtonActive;

        // Stores a couple of settings for detecting specific editor changes.
        [HideInInspector, SerializeField]
        private SettingsWatcher m_SettingsWatcher;

        // Sample arrays used for both amplitude and spectrum sampling.
        private float[] m_SamplesL;
        private float[] m_SamplesR;

        // Observed FFT bounds, min & max inclusive.
        private int m_FFTMinIndex;
        private int m_FFTMaxIndex;

        // FFT normalization.
        [HideInInspector, SerializeField]
        private float[] m_FFTExpansionFactors;
        private float[] m_FFTScaledPeaks;
        private bool HasNoFFTExpansionFactors => m_FFTExpansionFactors == null || m_FFTExpansionFactors.Length == 0;

        // Amplitude normalization.
        [HideInInspector, SerializeField]
        private float m_AmpExpansionFactor;
        private float m_AmpScaledPeak;
        private bool HasNoAmpExpansionFactor => m_AmpExpansionFactor == 0;

        // Ceiling for normalization, expansion factor = ceiling / peak.
        private const float c_Ceiling = 0.99f;
        private const float c_MinPeak = 0.01f;

        // Sampling step count < buffer length.
        private int m_SamplingStepCount;


        /// <inheritdoc/>
        public override int[] GetObservationShape()
        {
            return m_Shape.ToArray();
        }

        /// <inheritdoc/>
        public override ISensor CreateSensor()
        {
            Sensor = new AudioSensor(m_Shape, m_CompressionType, SensorName);
            Sensor.ResetEvent += OnSensorReset;
            return Sensor;
        }

        private void Awake()
        {
            Academy.Instance.AgentPreStep += OnAgentPreStep;
        }

        private void OnDestroy()
        {
            if (Academy.IsInitialized)
            {
                Academy.Instance.AgentPreStep -= OnAgentPreStep;
            }
            if (Sensor != null)
            {
                Sensor.ResetEvent -= OnSensorReset;
            }
        }

        private void OnAgentPreStep(int academyStepCount)
        {
            if (SamplingEnabled && Sensor != null)
            {
                Sensor.Buffer.SetChannel(m_SamplingStepCount);
                SampleAudio();
                SamplingUpdateEvent?.Invoke(m_SamplingStepCount, m_SamplingStepCount == m_BufferLength - 1);
                m_SamplingStepCount = ++m_SamplingStepCount % m_BufferLength;
            }
        }

        // Called via ISensor.Reset() at end of episode.
        private void OnSensorReset()
        {
            m_SamplingStepCount = 0;
        }


        #region Called by Custom Editor

        /// <summary>
        /// Calculates the root mean square of the current batch of amplitude samples.
        /// The batch size can be quite large, depending on the sample rate and update
        /// interval (1024 with 48kHz at 20ms FixedUpdate steps). Therefore the editor
        /// meter will hardly ever show levels going near the maximum, even if individual
        /// samples are clipping, because those are averaged out over the whole batch.
        /// </summary>
        /// <returns>Amplitude root mean square.</returns>
        public float[] GetRMSLevel()
        {
            return m_SignalType == SignalType.Mono
                ? GetRMSLevelMono()
                : GetRMSLevelStereo();
        }

        private float[] GetRMSLevelMono()
        {
            float sum = 0;
            for (int i = 0; i < SamplesPerChannel; i++)
            {
                float mean = GetLRMeanSampleValue(i);
                sum += mean * mean;
            }

            float rms = Mathf.Sqrt(sum / (float)SamplesPerChannel);
            return new float[] { rms, rms };
        }

        private float[] GetRMSLevelStereo()
        {
            float sumL = 0;
            float sumR = 0;
            for (int i = 0; i < SamplesPerChannel; i++)
            {
                sumL += m_SamplesL[i] * m_SamplesL[i];
                sumR += m_SamplesR[i] * m_SamplesR[i];
            }

            return new float[] {
            Mathf.Sqrt(sumL / (float)SamplesPerChannel),
            Mathf.Sqrt(sumR / (float)SamplesPerChannel)
        };
        }

        /// <summary>
        /// Calculates the mean of left and right channel samples.
        /// Can be negative for amplitude samples.
        /// </summary>
        /// <param name="index">Index of the sample.</param>
        /// <returns>Mean of left and right values.</returns>
        public float GetLRMeanSampleValue(int index)
        {
            return (m_SamplesL[index] + m_SamplesR[index]) * 0.5f;
        }

        /// <summary>
        /// Returns the expansion factor for a specific FFT 
        /// band which is used for normalizing sample values.
        /// </summary>
        /// <param name="index">Index of the sample.</param>
        /// <returns>Expansion factor.</returns>
        public float GetFFTBandExpansion(int index)
        {
            return m_FFTExpansionFactors[index];
        }

        /// <summary>
        /// Returns the expansion factor for amplitudes
        /// which is used for normalizing sample values.
        /// </summary>
        /// <returns>Expansion factor.</returns>
        public float GetAmplitudeExpansion()
        {
            return m_AmpExpansionFactor;
        }

        /// <summary>
        /// Resets the current expansion factors, clears measured peaks.
        /// </summary>
        public void ResetNormalization()
        {
            Debug.Log("Resetting Normalization, Sample Type: " + m_SampleType);

            switch (m_SampleType)
            {
                case SampleType.Spectrum:
                    m_FFTExpansionFactors = Enumerable.Repeat(1f, FFTResolution).ToArray();
                    m_FFTScaledPeaks = Enumerable.Repeat(c_MinPeak, FFTResolution).ToArray();
                    break;

                case SampleType.Amplitude:
                    m_AmpExpansionFactor = 1f;
                    m_AmpScaledPeak = c_MinPeak;
                    break;
            }
        }

        #endregion


        #region Settings Update

        private void OnValidate()
        {
            UpdateSettings();
        }

        private void UpdateSettings()
        {
            switch (m_SampleType)
            {
                case SampleType.Spectrum:
                    UpdateSpectrumSettings();
                    break;

                case SampleType.Amplitude:
                    UpdateAmplitudeSettings();
                    break;
            }

            m_BufferLength = Mathf.Clamp(m_BufferLength, 1, 100);
            m_Shape.SignalType = m_SignalType;
            m_Shape.BufferLength = m_BufferLength;

            if (Sensor != null)
            {
                Sensor.CompressionType = CompressionType;
                Sensor.Shape = m_Shape;
            }

            m_SettingsWatcher.StoreSettings(this);
            SettingsUpdateEvent?.Invoke(this);
        }

        private void UpdateSpectrumSettings()
        {
            if (m_SettingsWatcher.SampleSizeInvalid(this) || m_SamplesL == null)
            {
                AudioUtil.CalcHarmonic(FFTResolution);
                m_SamplesL = new float[FFTResolution];
                m_SamplesR = new float[FFTResolution];
            }

            // Frequency band -> FFT indices (min/max inclusive).
            m_FFTMinIndex = AudioUtil.FrequencyToSpectrumIndex(FFTMinFrequency);
            m_FFTMaxIndex = AudioUtil.FrequencyToSpectrumIndex(FFTMaxFrequency);

            SamplesPerChannel = m_FFTMaxIndex - m_FFTMinIndex + 1;
            SetShapeDimensions();

            // Normalization.
            if (m_SettingsWatcher.SpectrumNormalizationInvalid(this) || HasNoFFTExpansionFactors)
            {
                ResetNormalization();
            }
            else if (m_FFTScaledPeaks == null)
            {
                // Restore peaks from serialized factors.
                m_FFTScaledPeaks = new float[FFTResolution];
                for (int i = 0; i < m_FFTScaledPeaks.Length; i++)
                {
                    m_FFTScaledPeaks[i] = m_FFTExpansionFactors[i] > 1
                        ? c_Ceiling / m_FFTExpansionFactors[i] : c_MinPeak;
                }
            }
        }

        private void UpdateAmplitudeSettings()
        {
            if (m_SettingsWatcher.SampleSizeInvalid(this) || m_SamplesL == null)
            {
                // Would need to be adjusted for a sampling interval other than FixedUpdate.
                SamplesPerChannel = NextPowerOf2(AudioSettings.outputSampleRate * Time.fixedDeltaTime);
                SetShapeDimensions();

                m_SamplesL = new float[SamplesPerChannel];
                m_SamplesR = new float[SamplesPerChannel];
            }

            // Normalization.
            if (m_SettingsWatcher.AmplitudeNormalizationInvalid(this) || HasNoAmpExpansionFactor)
            {
                ResetNormalization();
            }
            else if (m_AmpScaledPeak == 0)
            {
                // Restore peak from serialized factor.
                m_AmpScaledPeak = m_AmpExpansionFactor > 1
                    ? c_Ceiling / m_AmpExpansionFactor : c_MinPeak;
            }
        }

        private void SetShapeDimensions()
        {
            // Find squarish shape.
            int w = Mathf.CeilToInt(Mathf.Sqrt(SamplesPerChannel));
            int h = w - (w * w - SamplesPerChannel) / w;
            m_Shape.Width = w;
            m_Shape.Height = h;
        }

        private static int NextPowerOf2(float n)
        {
            float log = Mathf.Floor(Mathf.Log(n, 2));
            return (int)Mathf.Pow(2, log + 1);
        }

        #endregion


        #region Sampling

        private void SampleAudio()
        {
            IsClipping = false;

            switch (m_SampleType)
            {
                case SampleType.Spectrum:
                    SampleSpectrum();
                    break;

                case SampleType.Amplitude:
                    SampleAmplitude();
                    break;
            }
        }

        // SPECTRUM

        private void SampleSpectrum()
        {
            switch (m_SignalType)
            {
                case SignalType.Mono:
                    SampleSpectrumMono();
                    break;

                case SignalType.Stereo:
                    SampleSpectrumStereo();
                    break;
            }
        }

        private void SampleSpectrumMono()
        {
            AudioListener.GetSpectrumData(m_SamplesL, 0, m_FFTWindow);
            AudioListener.GetSpectrumData(m_SamplesR, 1, m_FFTWindow);

            float floor = FFTFloor;
            var buffer = Sensor.Buffer;

            if (MeasurePeaks)
            {
                for (int i = 0, n = FFTResolution; i < n; i++)
                {
                    float scaled = AudioUtil.RescaleSampleValue(GetLRMeanSampleValue(i), floor);
                    IsClipping = IsClipping || scaled * m_FFTExpansionFactors[i] >= 1;
                    m_FFTScaledPeaks[i] = Mathf.Max(m_FFTScaledPeaks[i], scaled);
                    m_FFTExpansionFactors[i] = c_Ceiling / m_FFTScaledPeaks[i];
                }
            }

            for (int i = m_FFTMinIndex; i <= m_FFTMaxIndex; i++)
            {
                float scaled = AudioUtil.RescaleSampleValue(GetLRMeanSampleValue(i), floor);
                if (m_Normalize)
                {
                    scaled = Mathf.Clamp01(scaled * m_FFTExpansionFactors[i]);
                }

                buffer.AddSample(scaled);
            }
        }

        private void SampleSpectrumStereo()
        {
            AudioListener.GetSpectrumData(m_SamplesL, 0, m_FFTWindow);
            AudioListener.GetSpectrumData(m_SamplesR, 1, m_FFTWindow);

            float floor = FFTFloor;
            var buffer = Sensor.Buffer;

            if (MeasurePeaks)
            {
                for (int i = 0, n = FFTResolution; i < n; i++)
                {
                    float scaledL = AudioUtil.RescaleSampleValue(m_SamplesL[i], floor);
                    float scaledR = AudioUtil.RescaleSampleValue(m_SamplesR[i], floor);
                    IsClipping = IsClipping
                        || scaledL * m_FFTExpansionFactors[i] >= 1
                        || scaledR * m_FFTExpansionFactors[i] >= 1;
                    m_FFTScaledPeaks[i] = Mathf.Max(m_FFTScaledPeaks[i], scaledL);
                    m_FFTScaledPeaks[i] = Mathf.Max(m_FFTScaledPeaks[i], scaledR);
                    m_FFTExpansionFactors[i] = c_Ceiling / m_FFTScaledPeaks[i];
                }
            }

            for (int i = m_FFTMinIndex; i <= m_FFTMaxIndex; i++)
            {
                float scaledL = AudioUtil.RescaleSampleValue(m_SamplesL[i], floor);
                float scaledR = AudioUtil.RescaleSampleValue(m_SamplesR[i], floor);
                if (m_Normalize)
                {
                    scaledL = Mathf.Clamp01(scaledL * m_FFTExpansionFactors[i]);
                    scaledR = Mathf.Clamp01(scaledR * m_FFTExpansionFactors[i]);
                }

                buffer.AddSample(scaledL, scaledR);
            }
        }

        // AMPLITUDE

        private void SampleAmplitude()
        {
            switch (m_SignalType)
            {
                case SignalType.Mono:
                    SampleAmplitudeMono();
                    break;

                case SignalType.Stereo:
                    SampleAmplitudeStereo();
                    break;
            }
        }

        private void SampleAmplitudeMono()
        {
            AudioListener.GetOutputData(m_SamplesL, 0);
            AudioListener.GetOutputData(m_SamplesR, 1);

            float floor = AmpFloor;
            var buffer = Sensor.Buffer;

            if (MeasurePeaks)
            {
                for (int i = 0; i < SamplesPerChannel; i++)
                {
                    float mean = GetLRMeanSampleValue(i);
                    float scaled = AudioUtil.RescaleSampleValue(mean, floor);
                    IsClipping = IsClipping || scaled * m_AmpExpansionFactor >= 1;
                    m_AmpScaledPeak = Mathf.Max(m_AmpScaledPeak, scaled);
                }
                m_AmpExpansionFactor = c_Ceiling / m_AmpScaledPeak;
            }

            for (int i = 0; i < SamplesPerChannel; i++)
            {
                // Mean can be negative.
                float mean = GetLRMeanSampleValue(i);
                // Scaled is always positive.
                float scaled = AudioUtil.RescaleSampleValue(mean, floor);
                if (m_Normalize)
                {
                    scaled = Mathf.Clamp01(scaled * m_AmpExpansionFactor);
                }

                // -1/+1 -> 0/+1
                buffer.AddSample(0.5f + scaled * 0.5f * Mathf.Sign(mean));
            }
        }

        private void SampleAmplitudeStereo()
        {
            AudioListener.GetOutputData(m_SamplesL, 0);
            AudioListener.GetOutputData(m_SamplesR, 1);

            float floor = AmpFloor;
            var buffer = Sensor.Buffer;

            if (MeasurePeaks)
            {
                for (int i = 0; i < SamplesPerChannel; i++)
                {
                    float scaledL = AudioUtil.RescaleSampleValue(m_SamplesL[i], floor);
                    float scaledR = AudioUtil.RescaleSampleValue(m_SamplesR[i], floor);
                    IsClipping = IsClipping
                        || scaledL * m_AmpExpansionFactor >= 1
                        || scaledR * m_AmpExpansionFactor >= 1;
                    m_AmpScaledPeak = Mathf.Max(m_AmpScaledPeak, scaledL);
                    m_AmpScaledPeak = Mathf.Max(m_AmpScaledPeak, scaledR);
                }
                m_AmpExpansionFactor = c_Ceiling / m_AmpScaledPeak;
            }

            for (int i = 0; i < SamplesPerChannel; i++)
            {
                // Scaled is always positive.
                float scaledL = AudioUtil.RescaleSampleValue(m_SamplesL[i], floor);
                float scaledR = AudioUtil.RescaleSampleValue(m_SamplesR[i], floor);
                if (m_Normalize)
                {
                    scaledL = Mathf.Clamp01(scaledL * m_AmpExpansionFactor);
                    scaledR = Mathf.Clamp01(scaledR * m_AmpExpansionFactor);
                }

                // -1/+1 -> 0/+1
                buffer.AddSample(
                    0.5f + scaledL * 0.5f * Mathf.Sign(m_SamplesL[i]),
                    0.5f + scaledR * 0.5f * Mathf.Sign(m_SamplesR[i])
                );
            }
        }

        #endregion
    }
}