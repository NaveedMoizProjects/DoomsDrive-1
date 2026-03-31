using System.Collections.Generic;
using UnityEngine;

public class MeshDeformer : MonoBehaviour
{
    [Header("Target Visuals")]
    [Tooltip("If empty, this GameObject will be the only one deformed.")]
    public List<MeshFilter> targetBodies = new List<MeshFilter>();

    private struct MeshData
    {
        public Mesh mesh;
        public Vector3[] originalVertices;
        public Vector3[] displacedVertices;
        public Transform transform;
    }

    private List<MeshData> meshInstances = new List<MeshData>();

    [Header("Deformation Settings")]
    public float maxDeformDepth = 0.2f;
    public float globalFalloffPower = 2.0f;
    public float impactRadius = 0.5f;
    public float deformationHardness = 0.05f;

    void Start()
    {
        // If no targets assigned, add itself
        if (targetBodies.Count == 0)
        {
            MeshFilter selfFilter = GetComponent<MeshFilter>();
            if (selfFilter != null) targetBodies.Add(selfFilter);
        }

        // Initialize all meshes in the list
        foreach (var filter in targetBodies)
        {
            if (filter == null) continue;

            MeshData data = new MeshData();
            data.mesh = filter.mesh; // Creates an instance
            data.originalVertices = data.mesh.vertices;
            data.displacedVertices = (Vector3[])data.originalVertices.Clone();
            data.transform = filter.transform;

            meshInstances.Add(data);
        }
    }

    // This handles physical hits on the collider
    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        ContactPoint contact = collision.contacts[0];
        float power = impactForce * deformationHardness;

        DeformAllMeshes(contact.point, impactRadius, power, contact.normal);
    }

    public void DeformAllMeshes(Vector3 worldPoint, float worldRadius, float power, Vector3 hitNormal)
    {
        // Use a 'for' loop instead of 'foreach' to allow modification
        for (int j = 0; j < meshInstances.Count; j++)
        {
            // Get a local copy to work with
            MeshData data = meshInstances[j];

            Vector3 localPoint = data.transform.InverseTransformPoint(worldPoint);
            Vector3 localNormal = data.transform.InverseTransformDirection(hitNormal);
            float averageScale = (data.transform.lossyScale.x + data.transform.lossyScale.y + data.transform.lossyScale.z) / 3f;
            float localRadius = worldRadius / averageScale;

            bool hasChanged = false;

            for (int i = 0; i < data.displacedVertices.Length; i++)
            {
                float distance = Vector3.Distance(localPoint, data.displacedVertices[i]);

                if (distance < localRadius)
                {
                    float falloff = Mathf.Pow(1f - (distance / localRadius), globalFalloffPower);
                    Vector3 deformation = localNormal * (power * falloff);
                    Vector3 targetPos = data.displacedVertices[i] + deformation;

                    if (Vector3.Distance(data.originalVertices[i], targetPos) < maxDeformDepth)
                    {
                        data.displacedVertices[i] = targetPos;
                        hasChanged = true;
                    }
                }
            }

            if (hasChanged)
            {
                data.mesh.vertices = data.displacedVertices;
                data.mesh.RecalculateNormals();

                // Re-assign the modified struct back to the list
                meshInstances[j] = data;

                MeshCollider mc = data.transform.GetComponent<MeshCollider>();
                if (mc != null) mc.sharedMesh = data.mesh;
            }
        }
    }

    public void ResetDeformation()
    {
        for (int j = 0; j < meshInstances.Count; j++)
        {
            MeshData data = meshInstances[j];

            data.displacedVertices = (Vector3[])data.originalVertices.Clone();
            data.mesh.vertices = (Vector3[])data.originalVertices.Clone();
            data.mesh.RecalculateNormals();

            // Re-assign the reset struct back to the list
            meshInstances[j] = data;

            MeshCollider mc = data.transform.GetComponent<MeshCollider>();
            if (mc != null) mc.sharedMesh = data.mesh;
        }
    }
}