using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class CarHUD : MonoBehaviour
{
    [System.Serializable]
    public struct PartUIEntry
    {
        public DamageablePart.PartType type;
        public GameObject uiElement; // assign the existing UI element (slider, image, text) in the Inspector
    }

    [Header("Assign existing UI elements (one per PartType)")]
    public List<PartUIEntry> partUIEntries = new List<PartUIEntry>();

    // runtime lookup
    private Dictionary<DamageablePart.PartType, GameObject> uiMap;

    void Awake()
    {
        uiMap = new Dictionary<DamageablePart.PartType, GameObject>();
        foreach (var e in partUIEntries)
        {
            if (e.uiElement != null)
                uiMap[e.type] = e.uiElement;
        }
    }

    void Update()
    {
        if (DamageManager.Instance == null) return;

        // If HUD paused but registry is empty (respawn rebuild), allow update
        bool noData = DamageManager.Instance.carHealthRegistry.Count == 0;
        if (DamageManager.Instance.hudPaused && !noData) return;

        // Iterate the enum and update only assigned UI elements
        foreach (DamageablePart.PartType t in System.Enum.GetValues(typeof(DamageablePart.PartType)))
        {
            if (!uiMap.TryGetValue(t, out GameObject ui)) continue; // skip unassigned types

            // gather player-owned parts of this type
            var playerParts = DamageManager.Instance.carHealthRegistry.Values
                .Where(x => x.type == t &&
                            x.ownerCar != null &&
                            x.ownerCar.CompareTag("Player"))
                .ToList();

            float shownHealth = 100f;
            if (playerParts.Count > 0)
                shownHealth = playerParts.Average(x => x.health);

            UpdateUIForElement(ui, shownHealth);
        }
    }

    // Try to update the assigned UI element flexibly:
    // - Slider (sets value 0..100)
    // - Image (filled type: fillAmount 0..1, otherwise tint color)
    // - TextMeshProUGUI (percent text)
    void UpdateUIForElement(GameObject uiObj, float healthPercent)
    {
        if (uiObj == null) return;

        // 1) Slider (preferred for bars)
        Slider s = uiObj.GetComponent<Slider>();
        if (s != null)
        {
            // ensure slider scale is 0..100 for consistent behavior
            if (s.maxValue < 100f) s.maxValue = 100f;
            s.value = Mathf.Clamp(healthPercent, 0f, 100f);
        }

        // 2) Image fill (useful when UI has an Image fill)
        Image img = uiObj.GetComponent<Image>();
        if (img != null)
        {
            if (img.type == Image.Type.Filled)
            {
                img.fillAmount = Mathf.Clamp01(healthPercent / 100f);
            }
            else
            {
                // tint color as health indicator
                img.color = HealthColor(healthPercent);
            }
        }
        // 3) Support nested Text inside the provided uiObj
        TextMeshProUGUI tmp = uiObj.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = Mathf.CeilToInt(healthPercent) + "%";
            tmp.color = HealthColor(healthPercent);
        }

        // 4) If uiObj holds a child slider or image, above GetComponent calls already handle them.
    }

    // returns white/yellow/red per thresholds used previously
    Color HealthColor(float health)
    {
        if (health < 25f) return Color.red;
        if (health < 60f) return Color.yellow;
        return Color.white;
    }
}