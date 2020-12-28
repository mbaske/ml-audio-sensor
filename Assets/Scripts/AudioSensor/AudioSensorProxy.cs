using Unity.MLAgents.Sensors;

namespace AudioSensor
{
    /// <summary>
    /// Sensor proxy that refers to an <see cref="AudioSensor"/>.
    /// </summary>
    public class AudioSensorProxy : ISensor
    {
        public SensorObservationShape Shape => m_AudioSensor.Shape;
        public SensorCompressionType CompressionType => m_AudioSensor.CompressionType;

        private readonly AudioSensor m_AudioSensor;

        /// <summary>
        /// Initializes the sensor.
        /// </summary>
        /// <param name="audioSensor">The <see cref="AudioSensor"/> to refer to.</param>
        public AudioSensorProxy(AudioSensor audioSensor)
        {
            m_AudioSensor = audioSensor;
        }

        /// <inheritdoc/>
        public string GetName()
        {
            return m_AudioSensor.GetName() + "_Proxy";
        }

        /// <inheritdoc/>
        public int[] GetObservationShape()
        {
            return m_AudioSensor.Shape.ToArray();
        }

        /// <inheritdoc/>
        public SensorCompressionType GetCompressionType()
        {
            return m_AudioSensor.CompressionType;
        }

        /// <inheritdoc/>
        public byte[] GetCompressedObservation()
        {
            return m_AudioSensor.CachedCompressedObservation;
        }

        /// <inheritdoc/>
        public int Write(ObservationWriter writer)
        {
            return m_AudioSensor.Write(writer);
        }

        /// <inheritdoc/>
        public void Update() { }

        /// <inheritdoc/>
        public void Reset() { }
    }
}