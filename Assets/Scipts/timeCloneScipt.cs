using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class timeCloneScript : MonoBehaviour
{
    public List<Vector3> replayPositions;
    public float replayRate = 0.05f;

    public void StartReplay()
    {
        StartCoroutine(ReplayMovement());
    }


    IEnumerator ReplayMovement()
    {
        while (true) // Loop forever
        {
            for (int i = 0; i < replayPositions.Count; i++)
            {
                transform.position = replayPositions[i];
                yield return new WaitForSeconds(replayRate);
            }
        }
    }

}
