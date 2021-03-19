using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using MBaske.Sensors.Audio;

/// <summary>
/// Base class for agents listening to audio.
/// </summary>
public abstract class AudioAgent : Agent
{
    // Assuming there's only one audio sensor per agent.
    protected IAudioSampler m_Sampler;

    /// <inheritdoc/>
    public override void Initialize()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 50;

        m_Sampler = GetAudioSampler();
        m_Sampler.SamplingUpdateEvent += OnSamplingUpdate;
    }

    protected IAudioSampler GetAudioSampler()
    {
        if (m_Sampler == null)
        {
            var components = GetComponentsInChildren<SensorComponent>();
            foreach (var comp in components)
            {
                if (comp is IAudioSampler)
                {
                    return (IAudioSampler)comp;
                }
            }
            throw new MissingComponentException("Audio sensor component not found.");
        }

        return m_Sampler;
    }

    protected abstract void OnSamplingUpdate(int samplingStepCount, bool bufferLengthReached);

    /// <inheritdoc/>
    public override void Heuristic(in ActionBuffers actionsOut) { }

    private void OnDestroy()
    {
        if (m_Sampler != null)
        {
            m_Sampler.SamplingUpdateEvent -= OnSamplingUpdate;
        }
    }
}