using UnityEngine;
using Unity.MLAgents.Actuators;
using System.Collections;

/// <summary>
/// Recognizes spoken numbers. Pauses audio sampling during decision & action phase.
/// </summary>
public class SpeechRecognizingAgent : AudioAgent
{
    // Number of decisions & actions per episode,
    // independent of the audio buffer's length.
    [SerializeField]
    private int m_EpisodeLength = 100;
    private int m_DecisionCount;
    private int m_SuccessCount;

    [SerializeField, Range(0f, 1f)]
    private float m_PauseDuration;
    private const float c_EpisodeDelay = 1;

    private SpokenNumbersAudio m_NumbersAudio;
    private int m_SpokenNumber = -1;
    private int m_AgentGuess = -1;

    private GUIStyle m_GUIStyle;
    private GUIStyle m_GUIStyleMatch;
    private GUIStyle m_GUIStyleFail;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        m_NumbersAudio = FindObjectOfType<SpokenNumbersAudio>();
        m_NumbersAudio.RegisterAgent(this);
        InitGUIStyles();
    }

    /// <inheritdoc/>
    public override void OnEpisodeBegin()
    {
        m_SuccessCount = 0;
        m_DecisionCount = 0;
        StartCoroutine(DelayAgentReady(c_EpisodeDelay));
    }

    /// <summary>
    /// Called by <see cref="SpokenNumbersAudio"/> prior to speaking the next number.
    /// <param name="n">The spoken number.</param>
    /// </summary>
    public void OnNextNumber(int n)
    {
        // Continue audio sampling.
        m_Sampler.SamplingEnabled = true;
        m_SpokenNumber = n;
        m_AgentGuess = -1;
    }

    protected override void OnSamplingUpdate(int samplingStepCount, bool bufferLengthReached)
    {
        if (bufferLengthReached)
        {
            // Pause audio sampling.
            m_Sampler.SamplingEnabled = false;
            m_DecisionCount++;
            RequestDecision();
        }
    }

    /// <inheritdoc/>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        m_AgentGuess = actionBuffers.DiscreteActions[0];

        if (m_AgentGuess == m_SpokenNumber)
        {
            m_SuccessCount++;
            AddReward(1);
        }

        if (m_DecisionCount == m_EpisodeLength)
        {
            EndEpisode();
        }
        else
        {
            StartCoroutine(DelayAgentReady(m_PauseDuration));
        }
    }

    private IEnumerator DelayAgentReady(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        m_NumbersAudio.OnAgentReady();
    }

    /// <inheritdoc/>
    public override void Heuristic(in ActionBuffers actionsOut) { }

    private void InitGUIStyles()
    {
        m_GUIStyle = new GUIStyle
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 72
        };
        m_GUIStyle.normal.textColor = Color.white;

        m_GUIStyleMatch = new GUIStyle(m_GUIStyle);
        m_GUIStyleMatch.normal.textColor = Color.green;

        m_GUIStyleFail = new GUIStyle(m_GUIStyle);
        m_GUIStyleFail.normal.textColor = Color.red;
    }

    private void OnGUI()
    {
        Rect rect;

        if (m_SpokenNumber != -1)
        {
            rect = new Rect(20, 10, 600, 80);
            GUI.Label(rect, $"Spoken Number: {m_SpokenNumber}", m_GUIStyle);
        }

        if (m_AgentGuess != -1)
        {
            var style = m_AgentGuess == m_SpokenNumber ? m_GUIStyleMatch : m_GUIStyleFail;
            rect = new Rect(20, 90, 600, 80);
            GUI.Label(rect, $"Agent Guess: {m_AgentGuess}", style);
        }

        if (m_DecisionCount > 0)
        {
            var rate = m_SuccessCount / (float)m_DecisionCount * 100;
            rect = new Rect(20, 170, 600, 80);
            GUI.Label(rect, $"Success Rate: {string.Format("{0:0}", rate)}%", m_GUIStyle);
        }
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}