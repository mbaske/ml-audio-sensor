using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates spoken numbers.
/// </summary>
public class SpokenNumbersAudio : MonoBehaviour
{
    private enum Set
    {
        // Subsets of audio clips.
        Training, Inference
    }
    [SerializeField]
    private Set m_Set;

    private AudioSource m_Audio;
    private List<AudioClip>[] m_Clips;

    private int m_AgentCount;
    private List<SpeechRecognizingAgent> m_Agents;

    private void Awake()
    {
        m_Audio = GetComponent<AudioSource>();
        m_Clips = new List<AudioClip>[10];
        for (int i = 0; i < 10; i++)
        {
            m_Clips[i] = new List<AudioClip>();
        }

        var clips = Resources.LoadAll("Audio/Numbers/" + m_Set, typeof(AudioClip));
        foreach (var clip in clips)
        {
            m_Clips[short.Parse(clip.name)].Add((AudioClip)clip);
        }
    }

    /// <summary>
    /// Registers a <see cref="SpeechRecognizingAgent"/>.
    /// <param name="agent"><see cref="SpeechRecognizingAgent"/>.</param>
    /// </summary>
    public void RegisterAgent(SpeechRecognizingAgent agent)
    {
        m_Agents = m_Agents ?? new List<SpeechRecognizingAgent>();
        m_Agents.Add(agent);
    }

    /// <summary>
    /// Called by each <see cref="SpeechRecognizingAgent"/> when it is ready for the 
    /// next observation. Plays next audio clip when all registered agents are ready.
    /// </summary>
    public void OnAgentReady()
    {
        if (++m_AgentCount == m_Agents.Count)
        {
            int n = Random.Range(0, 10);
            foreach (var agent in m_Agents)
            {
                agent.OnNextNumber(n);
            }
            m_AgentCount = 0;
            // Pick random speaker.
            m_Audio.PlayOneShot(m_Clips[n][Random.Range(0, m_Clips[n].Count)]);
        }
    }
}
