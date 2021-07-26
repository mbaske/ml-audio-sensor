using Unity.MLAgents.Sensors;
using System;

namespace MBaske.Sensors.Audio
{
    /// <summary>
    /// Sensor component proxy that refers to an <see cref="AudioSensorComponent"/>.
    /// </summary>
    public class AudioSensorComponentProxy : SensorComponent, IAudioSampler
    {
        /// <inheritdoc/>
        /// <see cref="IAudioSampler"/>
        public event Action<int, bool> SamplingUpdateEvent;

        /// <inheritdoc/>
        /// <see cref="IAudioSampler"/>
        public bool SamplingEnabled
        {
            get { return m_AudioSensorComponent.SamplingEnabled; }
            set { m_AudioSensorComponent.SamplingEnabled = value; }
        }

        private AudioSensorComponent m_AudioSensorComponent;

        /// <inheritdoc/>
        public override ISensor[] CreateSensors()
        {
            FindAudioSensorComponent();
            return new ISensor[] { new AudioSensorProxy(m_AudioSensorComponent.Sensor) };
        }

        // Assuming there's only one AudioSensorComponent in the scene.
        private void FindAudioSensorComponent()
        {
            if (m_AudioSensorComponent == null)
            {
                m_AudioSensorComponent = FindObjectOfType<AudioSensorComponent>();
                m_AudioSensorComponent.SamplingUpdateEvent += ForwardSamplingUpdateEvent;
            }
        }

        private void OnDestroy()
        {
            if (m_AudioSensorComponent == null)
            {
                m_AudioSensorComponent.SamplingUpdateEvent -= ForwardSamplingUpdateEvent;
            }
        }

        private void ForwardSamplingUpdateEvent(int samplingStepCount, bool bufferLengthReached)
        {
            SamplingUpdateEvent?.Invoke(samplingStepCount, bufferLengthReached);
        }
    }
}