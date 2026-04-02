using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [Header("Auto-Initialize from Data")]
    public SurfaceEffectData effectLibrary;

    [Header("Manual Pools (For Bullets/Players)")]
    public List<Pool> customPools;

    [System.Serializable]
    public struct Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    private Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        Instance = this;
        InitializePools();
    }

    void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        // 1. AUTO-GENERATE pools from your Surface Settings
        if (effectLibrary != null)
        {
            foreach (var impact in effectLibrary.impactEffects)
            {
                CreatePool(impact.poolTag, impact.prefab, impact.poolSize);
            }
            // Create the default pool too
            CreatePool(effectLibrary.defaultPoolTag, effectLibrary.defaultPrefab, effectLibrary.defaultPoolSize);
        }

        // 2. GENERATE manual pools (Bullets, Rockets, etc.)
        foreach (var pool in customPools)
        {
            CreatePool(pool.tag, pool.prefab, pool.size);
        }
    }

    void CreatePool(string tag, GameObject prefab, int size)
    {
        if (string.IsNullOrEmpty(tag) || prefab == null || poolDictionary.ContainsKey(tag)) return;

        Queue<GameObject> objectPool = new Queue<GameObject>();
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            obj.transform.SetParent(this.transform);
            objectPool.Enqueue(obj);
        }
        poolDictionary.Add(tag, objectPool);
    }

    // Added optional 'activate' param. If false, caller must call SetActive(true) after configuring the instance.
    public GameObject SpawnFromPool(string tag, Vector3 pos, Quaternion rot, bool activate = true)
    {
        if (!poolDictionary.ContainsKey(tag)) return null;

        GameObject obj = poolDictionary[tag].Dequeue();
        // ensure object is deactivated while we position/configure it
        obj.SetActive(false);
        obj.transform.position = pos;
        obj.transform.rotation = rot;

        // return to queue immediately to preserve pool rotation
        poolDictionary[tag].Enqueue(obj);

        if (activate)
            obj.SetActive(true);

        return obj;
    }
}