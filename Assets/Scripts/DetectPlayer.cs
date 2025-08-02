using UnityEngine;
using UnityEngine.AI;

public class DetectPlayer : MonoBehaviour
{
    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    
    private void OnTriggerStay(Collider other)
    {
        Vector3 dir = other.transform.position;
        dir -= transform.position;
        RaycastHit hit;
        bool hasHit = Physics.Raycast(transform.position, dir,out hit);
        if (other.gameObject.tag == "Player" && hit.transform.tag == "Player")
        {
            Debug.Log(other.gameObject.tag);
            _agent.SetDestination(other.gameObject.transform.position);
        }
        

    }
}
