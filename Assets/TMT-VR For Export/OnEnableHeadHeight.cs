using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnEnableHeadHeight : MonoBehaviour
{
    public GameObject camHead;
    public Vector3 currentPosition;
    public float height;
    public float lower;
    public float higher;
    public bool withDelay;
    public float delay;
    public float appearTime;
    public bool high;
    public bool low;

    private void OnEnable()
    {
        if (withDelay)
        {
            appearTime = Time.time;
            StartCoroutine(UpdatePositionCoroutine());
        }
        else if (!withDelay)
        {
            currentPosition = gameObject.transform.position;
            if (high)
            {
                height = camHead.transform.position.y + higher;
            }
            else if (low)
            {
                height = camHead.transform.position.y - lower;
            }
            else { height = camHead.transform.position.y; }
            gameObject.transform.position = new Vector3(currentPosition.x, height, currentPosition.z);
        }
    }
    private IEnumerator UpdatePositionCoroutine()
    {
        // Wait until the condition is true
        yield return new WaitUntil(() => Time.time > appearTime + delay);

        // Once the condition is met, execute the following code
        currentPosition = gameObject.transform.position;
        if (high)
        {
            height = camHead.transform.position.y + higher;
        }
        else if (low)
        {
            height = camHead.transform.position.y - lower;
        }
        else
        {
            height = camHead.transform.position.y;
        }
        gameObject.transform.position = new Vector3(currentPosition.x, height, currentPosition.z);
    }
}
