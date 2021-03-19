using UnityEngine;

namespace MBaske.Sensors.Audio.Util
{
    /// <summary>
    /// Utility class for audio processing.
    /// </summary>
    public static class AudioUtil
    {
        public static float Harmonic;

        /// <summary>
        /// Calculates the harmonic based on spectrum size and sample rate.
        /// </summary>
        /// <param name="sprectrumSize">The number of FFT bands.</param>
        /// <returns>The harmonic value.</returns>
        public static float CalcHarmonic(int sprectrumSize)
        {
            Harmonic = 1 / (float)AudioSettings.outputSampleRate * sprectrumSize * 2;
            return Harmonic;
        }

        /// <summary>
        /// Calculates the frequency for a specific FFT band.
        /// </summary>
        /// <param name="index">The FFT band index.</param>
        /// <returns>The frequency.</returns>
        public static float SpectrumIndexToFrequency(int index)
        {
            return (index + 1) / Harmonic;
        }

        /// <summary>
        /// Calculates the FFT band index for a specific frequency.
        /// </summary>
        /// <param name="frequency">The frequency.</param>
        /// <returns>The FFT band index.</returns>
        public static int FrequencyToSpectrumIndex(float frequency)
        {
            return Mathf.Max(0, Mathf.RoundToInt(frequency * Harmonic) - 1);
        }

        /// <summary>
        /// Converts a decibel value to an amplitude between 0 and +1.
        /// </summary>
        /// <param name="dB">The decibel value.</param>
        /// <returns>Normalized amplitude.</returns>
        public static float DBToAmplitude(float dB)
        {
            return Mathf.Pow(10, dB / 20);
        }

        /// <summary>
        /// Converts an amplitude between -1 and +1 to a decibel value.
        /// </summary>
        /// <param name="amp">The normalized amplitude.</param>
        /// <returns>The decibel value.</returns>
        public static float AmplitudeToDB(float amp)
        {
            return 20 * Mathf.Log10(Mathf.Abs(amp));
        }

        /// <summary>
        /// Scales a sample value from -1 / +1 to a 0 / +1.
        /// </summary>
        /// <param name="value">The sample value.</param>
        /// <param name="floor">The minimum decibel value.</param>
        /// <returns>The scaled sample value.</returns>
        public static float RescaleSampleValue(float value, float floor = -60)
        {
            return Mathf.Max(AmplitudeToDB(value), floor) / -floor + 1;
        }

        /// <summary>
        /// Converts a frequency between 20Hz and 20kHz to a value between 0 and +1.
        /// </summary>
        /// <param name="frequency">The frequency.</param>
        /// <returns>The normalized value.</returns>
        public static float NormalizeFrequency(float frequency)
        {
            frequency = Mathf.Clamp(Mathf.Log10(frequency), Frequeny.Log20Hz, Frequeny.Log20kHz);
            return (frequency - Frequeny.Log20Hz) / Frequeny.LogRange;
        }

        /// <summary>
        /// Converts a value between 0 and +1 to a frequency between 20Hz and 20kHz.
        /// </summary>
        /// <param name="normalized">The normalized value.</param>
        /// <returns>The frequency.</returns>
        public static float NormalizedToFrequency(float normalized)
        {
            return Mathf.Pow(10, normalized * Frequeny.LogRange + Frequeny.Log20Hz);
        }
    }

    /// <summary>
    /// Frequeny log value constants.
    /// </summary>
    public static class Frequeny
    {
        public static float Log20Hz = Mathf.Log10(20);
        public static float Log20kHz = Mathf.Log10(20000);

        public static float LogRange = Log20kHz - Log20Hz;
    }
}