using UnityEngine;
using UnityEngine.SceneManagement;

public class Reset_Script : MonoBehaviour
{
    public float resetTime = 60f; // seconds
    private float timer;
    public float loopCount = 0;
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    public GameObject timeClone;

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
        GameObject clone = Instantiate(timeClone, startingPosition, startingRotation);
        DontDestroyOnLoad(clone);
    }
}


