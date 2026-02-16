using UnityEngine;

public class MeshDeformer : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] originalVertices, displacedVertices;
    private float sqrDeformRadius;

    [Header("Deformation Settings")]
    public float maxDeform = 0.4f;
    public float deformRadius = 1.5f;

    void Start()
    {
        // Important: Get a LOCAL instance of the mesh so we don't deform the original file
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        System.Array.Copy(originalVertices, displacedVertices, originalVertices.Length);

        sqrDeformRadius = deformRadius * deformRadius;
    }

    public void DeformMesh(Vector3 worldPoint, float radius, float power)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        float localSqrRadius = radius * radius;
        bool hasChanged = false;

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            float sqrMag = (displacedVertices[i] - localPoint).sqrMagnitude;

            if (sqrMag < localSqrRadius)
            {
                float distance = Mathf.Sqrt(sqrMag);
                float falloff = 1f - (distance / radius);

                // Push the vertex inward relative to the hit point
                Vector3 deformMove = (displacedVertices[i] - localPoint).normalized * (power * falloff);
                Vector3 newPos = displacedVertices[i] + deformMove;

                // Only apply if the total dent is within maxDeform limit
                if (Vector3.Distance(originalVertices[i], newPos) < maxDeform)
                {
                    displacedVertices[i] = newPos;
                    hasChanged = true;
                }
            }
        }

        if (hasChanged)
        {
            mesh.vertices = displacedVertices;
            mesh.RecalculateNormals(); // Crucial for showing the "dent" in the light
            // Optional: RecalculateBounds(); 
        }
    }
}