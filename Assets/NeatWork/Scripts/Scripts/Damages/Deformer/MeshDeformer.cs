using UnityEngine;
public class MeshDeformer : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] originalVertices, displacedVertices;

    [Header("Deformation Settings")]
    public float maxDeformDepth = 0.2f; // How deep the dent can go
    public float globalFalloffPower = 2.0f; // Higher = sharper dent, Lower = softer crater

    void Start()
    {
        // Get the mesh instance
        mesh = GetComponent<MeshFilter>().mesh;
        originalVertices = mesh.vertices;
        displacedVertices = (Vector3[])originalVertices.Clone();
    }

    public void DeformMesh(Vector3 worldPoint, float worldRadius, float power, Vector3 hitNormal)
    {
        // 1. Convert world impact point to local space
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        // 2. Adjust radius for local scale to prevent "spreading"
        // We take the average scale to keep it simple
        float averageScale = (transform.lossyScale.x + transform.lossyScale.y + transform.lossyScale.z) / 3f;
        float localRadius = worldRadius / averageScale;

        // 3. Convert the hit normal to local space so we know which way is "in"
        Vector3 localNormal = transform.InverseTransformDirection(hitNormal);

        bool hasChanged = false;

        for (int i = 0; i < displacedVertices.Length; i++)
        {
            float distance = Vector3.Distance(localPoint, displacedVertices[i]);

            if (distance < localRadius)
            {
                // Calculate Falloff (0 to 1)
                float normalizedDist = distance / localRadius;
                float falloff = Mathf.Pow(1f - normalizedDist, globalFalloffPower);

                // Calculate the potential new position
                // We push ALONG the hit normal (inward) rather than away from the point
                Vector3 deformation = localNormal * (power * falloff);
                Vector3 targetPos = displacedVertices[i] + deformation;

                // SAFETY: Check distance from original vertex to prevent infinite deforming
                float currentDepth = Vector3.Distance(originalVertices[i], targetPos);

                if (currentDepth < maxDeformDepth)
                {
                    displacedVertices[i] = targetPos;
                    hasChanged = true;
                }
            }
        }

        if (hasChanged)
        {
            mesh.vertices = displacedVertices;
            mesh.RecalculateNormals();
            // Crucial if your mesh has a MeshCollider!
            if (GetComponent<MeshCollider>()) GetComponent<MeshCollider>().sharedMesh = mesh;
        }
    }
}