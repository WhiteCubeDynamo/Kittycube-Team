#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [Header("View Settings")]
    public float viewRadius = 10f;
    [Range(0, 360)]
    public float viewAngle = 90f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [Header("Visualization")]
    public Material viewMaterial;
    public Color normalColor = new Color(1f, 1f, 0f, 0.2f);
    public Color detectedColor = new Color(1f, 0f, 0f, 0.4f);
    public int meshResolution = 4; // Number of rays per degree

    private MeshFilter _viewMeshFilter;
    private MeshRenderer _viewMeshRenderer;
    private Mesh _viewMesh;
    private bool _playerDetected = false;

    void Start()
    {
        // Create a child GameObject for the vision cone mesh
        GameObject viewObject = new GameObject("ViewCone");
        viewObject.transform.SetParent(transform, false);
        _viewMeshRenderer = viewObject.AddComponent<MeshRenderer>();
        _viewMeshFilter = viewObject.AddComponent<MeshFilter>();
        
        _viewMesh = new Mesh { name = "Vision Cone" };
        _viewMeshFilter.mesh = _viewMesh;
        if (viewMaterial != null)
        {
            _viewMeshRenderer.material = viewMaterial;
            _viewMeshRenderer.material.color = normalColor;
        }
    }

    void LateUpdate()
    {
        DrawFieldOfView();
        CheckForPlayer();
    }

    private void DrawFieldOfView()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;
        
        var viewPoints = new System.Collections.Generic.List<Vector3>();
        
        // Find all hit points for the vision cone
        for (int i = 0; i <= stepCount; i++)
        {
            float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngleSize * i;
            Vector3 dir = DirFromAngle(angle, true);
            
            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, viewRadius, obstacleMask))
            {
                viewPoints.Add(transform.InverseTransformPoint(hit.point));
            }
            else
            {
                viewPoints.Add(transform.InverseTransformPoint(transform.position + dir * viewRadius));
            }
        }

        // Create the mesh vertices and triangles
        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = viewPoints[i];

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        _viewMesh.Clear();
        _viewMesh.vertices = vertices;
        _viewMesh.triangles = triangles;
        _viewMesh.RecalculateNormals();
    }

    private void CheckForPlayer()
    {
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        _playerDetected = false; // Reset detection status each frame

        foreach (var target in targetsInViewRadius)
        {
            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float distToTarget = Vector3.Distance(transform.position, target.transform.position);
                if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                {
                    _playerDetected = true;
                    break; // Player is visible
                }
            }
        }

        // Update the material color based on detection status
        if (viewMaterial != null)
        {
            _viewMeshRenderer.material.color = _playerDetected ? detectedColor : normalColor;
        }
    }

    // Helper to get direction from an angle
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

#if UNITY_EDITOR
    // Draw gizmos in the editor for easier setup
    void OnDrawGizmos()
    {
        if (UnityEditor.Selection.activeGameObject != gameObject) return;

        Gizmos.color = _playerDetected ? detectedColor : normalColor;
        Handles.color = Gizmos.color;

        // Draw the view angle lines
        Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
        Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);
        Handles.DrawWireArc(transform.position, Vector3.up, viewAngleA, viewAngle, viewRadius);
        Handles.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
        Handles.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);
        
        // Draw a line to any detected player
        if (_playerDetected)
        {
            Gizmos.color = Color.red;
            Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
            foreach (var target in targetsInViewRadius)
            {
                 Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
                 if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
                 {
                    float distToTarget = Vector3.Distance(transform.position, target.transform.position);
                    if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                    {
                        Gizmos.DrawLine(transform.position, target.transform.position);
                    }
                 }
            }
        }
    }
#endif
}