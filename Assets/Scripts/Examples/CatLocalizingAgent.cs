using UnityEngine;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using MBaske.Sensors.Audio;

/// <summary>
/// Listens to continuous audio.
/// </summary>
public class CatLocalizingAgent : AudioAgent
{
    [SerializeField]
    private int m_DecisionInterval;
    private int m_BufferLength;

    [SerializeField]
    private Animal m_Cat;
    [SerializeField]
    private Animal m_Dog;

    private float m_Angle;

    /// <inheritdoc/>
    public override void OnEpisodeBegin()
    {
        m_Angle = 0;
        m_Cat.ResetPosition();
        m_Dog.ResetPosition();

        m_Sampler.SamplingEnabled = true;
    }

    protected override void OnSamplingUpdate(int samplingStepCount, bool bufferLengthReached)
    {
        if (samplingStepCount % m_DecisionInterval == 0)
        {
            // Will also request action.
            RequestDecision();
        }
        else
        {
            // Act in between decisions.
            RequestAction();
        }
    }

    /// <inheritdoc/>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float targetAngle = actionBuffers.ContinuousActions[0] * 90;
        m_Angle = Mathf.Lerp(m_Angle, targetAngle, Time.fixedDeltaTime);
        transform.eulerAngles = new Vector3(0, m_Angle, 0);

        float angleToCat = Vector3.Angle(transform.forward, m_Cat.transform.position - transform.position);
        AddReward(Mathf.Pow(1 - angleToCat / 180, 8));
    }

    /// <inheritdoc/>
    public override void Heuristic(in ActionBuffers actionsOut) 
    {
        var actions = actionsOut.ContinuousActions;
        actions[0] = Input.GetAxis("Horizontal");
    }


    // Settings validation.

    private void OnValidate()
    {
        var component = GetComponentInChildren<AudioSensorComponent>();
        // https://stackoverflow.com/a/7065771
        component.SettingsUpdateEvent -= OnSensorSettingsUpdate;
        component.SettingsUpdateEvent += OnSensorSettingsUpdate;
        m_BufferLength = component.BufferLength;
        ValidateIntervals();
    }

    private void OnSensorSettingsUpdate(AudioSensorComponent component)
    {
        m_BufferLength = component.BufferLength;
        ValidateIntervals();
    }

    private void ValidateIntervals()
    {
        m_DecisionInterval = Mathf.Clamp(m_DecisionInterval, 1, m_BufferLength);

        if (m_BufferLength % m_DecisionInterval != 0)
        {
            var divisors = new List<int>();
            for (int i = 1; i <= m_BufferLength; i++)
            {
                if (m_BufferLength % i == 0)
                {
                    divisors.Add(i);
                }
            }
            Debug.LogWarning($"Decision interval should be a fraction of the audio buffer's length {m_BufferLength}: "
                + string.Join(", ", divisors.ToArray()));
        }

        if (MaxStep % m_BufferLength != 0)
        {
            Debug.LogWarning($"Max Step should be a multiple of the audio buffer's length {m_BufferLength}.");
        }
    }
}