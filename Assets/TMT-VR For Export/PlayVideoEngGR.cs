using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class PlayVideoEngGR : MonoBehaviour
{
    public VideoPlayer videoPlayer; // Reference to the VideoPlayer component
    public VideoClip EnglishInstruction; // The video clip you want to play
    public VideoClip GreekInstruction; // The video clip you want to play
    public bool isGreek;
    public bool isEnglish;
    public float delayInSeconds; // The delay before the video plays

    private void OnEnable()
    {
        StartCoroutine(PlayVideoAfterDelay());
    }

    IEnumerator PlayVideoAfterDelay()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delayInSeconds);

        // Play the video clip
        if (isEnglish)
        {
            videoPlayer.clip = EnglishInstruction;
        }
        else if (isGreek)
        {
            videoPlayer.clip = GreekInstruction;
        }
        videoPlayer.Play();
    }
}
