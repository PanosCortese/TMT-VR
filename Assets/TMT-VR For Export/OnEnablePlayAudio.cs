using System;
using System.Collections;
using UnityEngine;

public class OnEnablePlayAudio : MonoBehaviour
{
    public enum Language {English, Greek};
    public Language language;
    private AudioSource m_AudioSource;
    public AudioClip engInstruct;
    public AudioClip grInstruct;
    public float delayTime;
    private void OnEnable()
    {
        m_AudioSource = GetComponent<AudioSource>();
        StartCoroutine(DelayToPlay());
    }

    private IEnumerator DelayToPlay()
    {
        yield return new WaitForSeconds(delayTime);
        switch (language)
        {
            case Language.English: m_AudioSource.PlayOneShot(engInstruct); break;
            case Language.Greek: m_AudioSource.PlayOneShot(grInstruct); break;
        }
    }
}
