using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays meows or barks audio clips, 
/// moves around randomly.
/// </summary>
public class Animal : MonoBehaviour
{
    private enum Set
    {
        // Subsets of audio clips.
        Cats, Dogs
    }
    [SerializeField]
    private Set m_Set;
    private AudioSource m_Audio;
    private List<AudioClip> m_Clips;

    private Transform m_Model;
    private Vector3 m_StartPosition;
    private Vector3 m_TargetPosition;
    private float m_TargetX;

    private void Awake()
    {
        m_Audio = GetComponent<AudioSource>();
        m_Clips = new List<AudioClip>();
        var clips = Resources.LoadAll("Audio/" + m_Set, typeof(AudioClip));
        foreach (var clip in clips)
        {
            m_Clips.Add((AudioClip)clip);
        }
        StartCoroutine(PlayRandomClip());

        m_Model = transform.GetChild(0);
        m_StartPosition = transform.position;
        m_TargetX = m_StartPosition.x;
    }

    private IEnumerator PlayRandomClip()
    {
        var clip = m_Clips[Random.Range(0, m_Clips.Count)];
        m_Audio.PlayOneShot(clip);
        yield return new WaitForSecondsRealtime(clip.length);
        StartCoroutine(PlayRandomClip());
    }

    public void ResetPosition()
    {
        m_StartPosition.x *= -1;
        transform.position = m_StartPosition;
        m_TargetX = m_StartPosition.x;
        RandomizeTarget();
    }

    private void Update()
    {
        if (Mathf.Abs(transform.position.x - m_TargetX) < 0.1f)
        {
            RandomizeTarget();
        }

        Vector3 delta = m_TargetPosition - transform.position;
        transform.Translate(delta.normalized * Time.deltaTime);
        m_Model.localRotation = Quaternion.LookRotation(delta);
    }

    private void RandomizeTarget()
    {
        m_TargetX *= -1;
        m_TargetPosition = new Vector3(m_TargetX, 0, Random.Range(-1f, 4f));
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
}
