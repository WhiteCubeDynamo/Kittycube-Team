using System.Collections.Generic;
using UnityEngine;

public class recordPlayer : MonoBehaviour
{
    public List<Vector3> recordedPositions = new List<Vector3>();
    public float recordRate = 0.05f; // Record every 0.05s

    private bool isRecording = true;

    void Start()
    {
        InvokeRepeating(nameof(RecordPosition), 0f, recordRate);
    }

    void RecordPosition()
    {
        if (isRecording)
            recordedPositions.Add(transform.position);
    }

    public void StopRecording()
    {
        isRecording = false;
    }
}