using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class TMTVR_InstructionsManager : MonoBehaviour
{
    public VideoPlayer videoPlayer; // Reference to the VideoPlayer component
    public bool Completed = false;

    private void OnEnable()
    {
        videoPlayer = gameObject.GetComponent<VideoPlayer>();
        // Start the coroutine to check video completion
        StartCoroutine(WaitForVideoEndCoroutine());
    }

    IEnumerator WaitForVideoEndCoroutine()
    {
        // Wait until video starts playing
        yield return new WaitUntil(() => videoPlayer.isPlaying);

        // Now wait until video stops playing
        yield return new WaitUntil(() => !videoPlayer.isPlaying);

        // Video has finished playing
        Completed = true;
    }
}
