using System;

namespace MBaske.Sensors.Audio
{
    /// <summary>
    /// Interface implemented by <see cref="AudioSensorComponent"/> 
    /// and <see cref="AudioSensorComponentProxy"/>.
    /// </summary>
    public interface IAudioSampler
    {
        /// <summary>
        /// Event is triggered at every update step (AgentPreStep) after sampling.
        /// Contains the sampling step count for the current sample batch and a bool
        /// indicating whether the step count reached the buffer length.
        /// </summary>
        event Action<int, bool> SamplingUpdateEvent;

        /// <summary>
        /// Whether audio is sampled on update steps (AgentPreStep).
        /// </summary>
        bool SamplingEnabled { get; set; }
    }
}