using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class Reset_Script : MonoBehaviour
{
    public float resetTime = 60f; // seconds
    private float timer;
    public float loopCount = 0;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    public GameObject timeClone;
    public recordPlayer recorder;

    void Start()
    {
        timer = resetTime;
        Debug.Log(loopCount);
        startingPosition = transform.position;
        startingRotation = transform.rotation;


    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            loopCount ++;
            spawnClone();
            
        
        }
        
    }

    void spawnClone()
    {
        GameObject clone = Instantiate(timeClone,  startingPosition, startingRotation);
        DontDestroyOnLoad(clone);

        // clone.layer = LayerMask.NameToLayer("Clone");
        // SetLayerRecursively(clone, LayerMask.NameToLayer("Clone"));


        var replayScript = clone.GetComponent<timeCloneScript>();
        replayScript.replayPositions = new List<Vector3>(recorder.recordedPositions);
        replayScript.StartReplay();
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

}


