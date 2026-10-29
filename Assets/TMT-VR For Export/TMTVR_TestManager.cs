using System.Collections;
using UnityEngine;

public class TMTVR_TestManager : MonoBehaviour
{
    [Header("Run tracking (real runs only)")]
    public int RealRunsA_Completed = 0;
    public int RealRunsB_Completed = 0;

    [Header("TMT-VR Managers")]
    public OpenXRAimPointer aimPointer;
    public TMTVRManager partA_Manager;
    public TMTVRManager partB_Manager;
    public TMTVRManager tutorial_partA_Manager;
    public TMTVRManager tutorial_partB_Manager;
    //public TMTVR_InstructionsManager instructionsA_Manager; // Instructions videos are now replaced by audio instructions, but keeping these variables in case we want to revert back to videos in the future
    //public TMTVR_InstructionsManager instructionsB_Manager; // Instructions videos are now replaced by audio instructions, but keeping these variables in case we want to revert back to videos in the future

    [Header("TMT-VR PartA and PartB completion tracking")]
    //public bool isfirstTMT_PartA = true;// We only want to show instructions the first time participants start Part A, but not the second time when they repeat Part A after completing Part B, so we can use this variable to track whether it's the first time starting Part A or not. Instructions videos are now replaced by audio instructions, but keeping this variable in case we want to revert back to videos in the future
    //public bool isfirstTMT_PartB = true; // We only want to show instructions the first time participants start Part B, but not the second time when they repeat Part B after completing Part A, so we can use this variable to track whether it's the first time starting Part B or not. Instructions videos are now replaced by audio instructions, but keeping this variable in case we want to revert back to videos in the future
    public bool TMT_PartAcompleted;
    public bool TMT_PartBcompleted;

    [Header("Instructions")]
    public GameObject InstructionsA;
    public GameObject InstructionsB;
    public GameObject InstructionsA_lang;
    public GameObject InstructionsB_lang;

    [Header("General Audio")]
    public AudioSource audioSource;
    public AudioClip TMT_Completed_audio;
    public bool isEnglish;

    [Header("Audio Files ENG")]
    public AudioClip General_Instructions;
    [Space(20)]
    public AudioClip Tutorial_A_EyeTracking;
    public AudioClip Tutorial_A_Head;
    public AudioClip Tutorial_A_Pointer;
    [Space(20)]
    public AudioClip Practice_A;
    public AudioClip Actual_A_Start;
    [Space(20)]
    public AudioClip Tutorial_B_EyeTracking;
    public AudioClip Tutorial_B_Head;
    public AudioClip Tutorial_B_Pointer;
    [Space(20)]
    public AudioClip Practice_B;
    public AudioClip Actual_B_Start;

    [Header("Audio Files Other Lang")]
    public AudioClip General_Instructions_lang;
    [Space(20)]
    public AudioClip Tutorial_A_EyeTracking_lang;
    public AudioClip Tutorial_A_Head_lang;
    public AudioClip Tutorial_A_Pointer_lang;
    [Space(20)]
    public AudioClip Practice_A_lang;
    public AudioClip Actual_A_Start_lang;
    [Space(20)]
    public AudioClip Tutorial_B_EyeTracking_lang;
    public AudioClip Tutorial_B_Head_lang;
    public AudioClip Tutorial_B_Pointer_lang;
    [Space(20)]
    public AudioClip Practice_B_lang;
    public AudioClip Actual_B_Start_lang;

    private void OnEnable()
    {
        TMT_PartAcompleted = false;
        TMT_PartBcompleted = false;
    }

    private void OnDisable()
    {
        TMT_PartAcompleted = false;
        TMT_PartBcompleted = false;
    }

    public void TMTA() => StartCoroutine(TMT_PartA());
    public void TMTB() => StartCoroutine(TMT_PartB());

    private AudioClip GetPartATutorialClip()
    {
        if (aimPointer == null)
        {
            if (isEnglish)
            {
                return Tutorial_A_Head; // default to head tutorial in English
            }
            else
            {
                return Tutorial_A_Head_lang; // default to head tutorial in other language
            }
        }
        else if (aimPointer.Eye)
        {
            if (isEnglish)
            {
                return Tutorial_A_EyeTracking;
            }
            else
            {
                return Tutorial_A_EyeTracking_lang;
            }
        }
        else if (aimPointer.Head)
        {
            if (isEnglish)
            {
                return Tutorial_A_Head;
            }
            else
            {
                return Tutorial_A_Head_lang;
            }
        }
        else if (aimPointer.Hand || aimPointer.Controller)
        {
            if (isEnglish)
            {
                return Tutorial_A_Pointer;
            }
            else
            {
                return Tutorial_A_Pointer_lang;
            }
        }
        else
        {
            if (isEnglish)
            {
                return Tutorial_A_Head; // default to head tutorial in English
            }
            else
            {
                return Tutorial_A_Head_lang; // default to head tutorial in other language
            }
        }
    }

    private AudioClip GetPartBTutorialClip()
    {
        if (aimPointer == null)
        {
            if (isEnglish)
            {
                return Tutorial_B_Head; // default to head tutorial in English
            }
            else
            {
                return Tutorial_B_Head_lang; // default to head tutorial in other language
            }
        }
        else if (aimPointer.Eye)
        {
            if (isEnglish)
            {
                return Tutorial_B_EyeTracking;
            }
            else
            {
                return Tutorial_B_EyeTracking_lang;
            }
        }
        else if (aimPointer.Head)
        {
            if (isEnglish)
            {
                return Tutorial_B_Head;
            }
            else
            {
                return Tutorial_B_Head_lang;
            }
        }
        else if (aimPointer.Hand || aimPointer.Controller)
        {
            if (isEnglish)
            {
                return Tutorial_B_Pointer;
            }
            else
            {
                return Tutorial_B_Pointer_lang;
            }
        }
        else
        {
            if (isEnglish)
            {
                return Tutorial_B_Head; // default to head tutorial in English
            }
            else
            {
                return Tutorial_B_Head_lang; // default to head tutorial in other language
            }
        }
    }

    public IEnumerator TMT_PartA()
    {
        /* OLD CODE: Instructions video is now replaced by audio instructions, 
         * but keeping this code in case we want to revert back to videos in the future
        if (isfirstTMT_PartA)
        {
            isfirstTMT_PartA = false;
            instructionsA_Manager.gameObject.SetActive(true);
            yield return new WaitUntil(() => instructionsA_Manager.Completed);
            instructionsA_Manager.gameObject.SetActive(false);
        }
        */

        yield return new WaitForSeconds(1f); // slight delay before instructions start
        if (isEnglish)
        {
            audioSource.clip = General_Instructions;
        }
        else
        {
            audioSource.clip = General_Instructions_lang;
        }
        audioSource.Play();
        yield return new WaitUntil(() => !audioSource.isPlaying);

        yield return new WaitForSeconds(1f); // slight delay before instructions start
        audioSource.clip = GetPartATutorialClip();
        audioSource.Play();
        yield return new WaitUntil(() => !audioSource.isPlaying);

        tutorial_partA_Manager.gameObject.SetActive(true);
        yield return new WaitUntil(() => tutorial_partA_Manager.Completed);
        tutorial_partA_Manager.gameObject.SetActive(false);

        // ---------------------------
        // Real Run #1 (Part A)
        // ---------------------------
        if (isEnglish)
        {
            audioSource.clip =  Practice_A;
        }
        else
        {
            audioSource.clip = Practice_A_lang;
        }
        yield return new WaitForSeconds(1f); // slight delay before instructions start
        audioSource.Play();
        yield return new WaitUntil(() => !audioSource.isPlaying);

        partA_Manager.gameObject.SetActive(true);
        if (isEnglish)
        {
            InstructionsA.SetActive(true);
        }
        else 
        {
            InstructionsA_lang.SetActive(true);
        }
        yield return new WaitUntil(() => partA_Manager.Completed);
        RealRunsA_Completed++;

        InstructionsA.SetActive(false);
        InstructionsA_lang.SetActive(false);

        yield return new WaitForSeconds(1.0f);
        audioSource.clip = TMT_Completed_audio;
        audioSource.Play();
        yield return new WaitUntil(() => !audioSource.isPlaying);

        partA_Manager.gameObject.SetActive(false);

        // ---------------------------
        // Real Run #2 (Part A)
        // ---------------------------
        if (isEnglish)
        {
            audioSource.clip = Actual_A_Start;
        }
        else
        {
            audioSource.clip = Actual_A_Start_lang;
        }
        yield return new WaitForSeconds(1f); // slight delay before instructions start
        audioSource.Play();
        yield return new WaitUntil(() => !audioSource.isPlaying);

        partA_Manager.gameObject.SetActive(true);
        yield return new WaitUntil(() => partA_Manager.Completed);
        RealRunsA_Completed++;

        yield return new WaitForSeconds(1.0f);
        audioSource.clip = TMT_Completed_audio;
        audioSource.Play();
        yield return new WaitUntil(() => !audioSource.isPlaying);

        partA_Manager.gameObject.SetActive(false);

        TMT_PartAcompleted = true;
        yield break;
    }

    public IEnumerator TMT_PartB()
    {
        /* OLD CODE: Instructions video is now replaced by audio instructions, 
         * but keeping this code in case we want to revert back to videos in the future
         * 
         * Note that we are not showing instructions again before the second part, but if we decide to show instructions again before the second part, we can use this code to do so.
         * We would just need to reset the isfirstTMT_PartB variable to true when starting the second part, so that instructions will be shown again before the second part.
         * This way, if participants want to do a second part B run after completing the first part B run, they can see the instructions again before starting the second part B run.
        
        if (isfirstTMT_PartB)
        {
            isfirstTMT_PartB = false;
            instructionsB_Manager.gameObject.SetActive(true);
            yield return new WaitUntil(() => instructionsB_Manager.Completed);
            instructionsB_Manager.gameObject.SetActive(false);
        }
         */
        yield return new WaitForSeconds(1f); // slight delay before instructions start
        audioSource.clip = GetPartBTutorialClip();
        audioSource.Play();
        yield return new WaitUntil(() => !audioSource.isPlaying);

        tutorial_partB_Manager.gameObject.SetActive(true);
        yield return new WaitUntil(() => tutorial_partB_Manager.Completed);
        tutorial_partB_Manager.gameObject.SetActive(false);

        // ---------------------------
        // Real Run #1 (Part B)
        // ---------------------------
        
        if (isEnglish)
        {
            audioSource.clip = Practice_B;
        }
        else
        {
            audioSource.clip = Practice_B_lang;
        }
        yield return new WaitForSeconds(1f); // slight delay before instructions start
        audioSource.Play();
        yield return new WaitUntil(() => !audioSource.isPlaying);

        partB_Manager.gameObject.SetActive(true);
        if (isEnglish)
        {
            InstructionsB.SetActive(true);
        }
        else
        {
            InstructionsB_lang.SetActive(true);
        }
        yield return new WaitUntil(() => partB_Manager.Completed);
        RealRunsB_Completed++;

        InstructionsB.SetActive(false);
        InstructionsB_lang.SetActive(false);

        yield return new WaitForSeconds(1.0f);
        audioSource.clip = TMT_Completed_audio;
        audioSource.Play();
        yield return new WaitUntil(() => !audioSource.isPlaying);

        partB_Manager.gameObject.SetActive(false);

        // ---------------------------
        // Real Run #2 (Part B)
        // ---------------------------
        if (isEnglish)
        {
            audioSource.clip = Actual_B_Start;
        }
        else
        {
            audioSource.clip = Actual_B_Start_lang;
        }
        yield return new WaitForSeconds(1f); // slight delay before instructions start
        audioSource.Play();
        yield return new WaitUntil(() => !audioSource.isPlaying);

        partB_Manager.gameObject.SetActive(true);
        yield return new WaitUntil(() => partB_Manager.Completed);
        RealRunsB_Completed++;

        yield return new WaitForSeconds(1.0f);
        audioSource.clip = TMT_Completed_audio;
        audioSource.Play();
        yield return new WaitUntil(() => !audioSource.isPlaying);

        partB_Manager.gameObject.SetActive(false);

        TMT_PartBcompleted = true;
        yield break;
    }
}