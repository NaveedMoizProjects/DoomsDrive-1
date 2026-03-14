using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SurfaceSettings", menuName = "Combat/Surface Settings")]
public class SurfaceEffectData : ScriptableObject
{
    [System.Serializable]
    public struct SurfaceImpact
    {
        public string surfaceTag; // e.g. "Terrain"
        public string poolTag;    // e.g. "GunEffectonTerrain"
        public GameObject prefab; // The actual VFX Prefab
        public int poolSize;      // How many to pool
    }

    public List<SurfaceImpact> impactEffects;

    [Header("Fallback")]
    public string defaultPoolTag = "DefaultImpact";
    public GameObject defaultPrefab;
    public int defaultPoolSize = 10;

    public string GetPoolTagForSurface(string hitTag)
    {
        foreach (var impact in impactEffects)
        {
            if (impact.surfaceTag == hitTag) return impact.poolTag;
        }
        return defaultPoolTag;
    }
}