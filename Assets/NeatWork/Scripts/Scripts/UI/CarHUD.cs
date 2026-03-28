using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CarHUD : MonoBehaviour
{
    public Transform container;
    public GameObject rowPrefab;

    private Dictionary<DamageablePart.PartType, TextMeshProUGUI> uiEntries =
        new Dictionary<DamageablePart.PartType, TextMeshProUGUI>();

    void Update()
    {
        if (DamageManager.Instance == null) return;

        // If HUD paused but registry is empty (respawn rebuild), allow update
        bool noData = DamageManager.Instance.carHealthRegistry.Count == 0;
        if (DamageManager.Instance.hudPaused && !noData) return;

        foreach (DamageablePart.PartType t in System.Enum.GetValues(typeof(DamageablePart.PartType)))
        {
            if (!uiEntries.ContainsKey(t))
                CreateNewRow(t);

            var playerParts = DamageManager.Instance.carHealthRegistry.Values
                .Where(x => x.type == t &&
                            x.ownerCar != null &&
                            x.ownerCar.CompareTag("Player"))
                .ToList();

            if (playerParts.Count > 0)
            {
                float avgHealth = playerParts.Average(x => x.health);
                UpdateRowVisuals(t, avgHealth);
            }
            else
            {
                // Important fallback: show full health instead of stale/empty
                UpdateRowVisuals(t, 100f);
            }
        }
    }

    void UpdateRowVisuals(DamageablePart.PartType type, float health)
    {
        if (!uiEntries.ContainsKey(type)) return;

        var text = uiEntries[type];
        text.text = Mathf.CeilToInt(health) + "%";

        if (health < 25f) text.color = Color.red;
        else if (health < 60f) text.color = Color.yellow;
        else text.color = Color.white;
    }

    void CreateNewRow(DamageablePart.PartType type)
    {
        GameObject newRow = Instantiate(rowPrefab, container);

        TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length >= 2)
        {
            texts[0].text = type.ToString().ToUpper();
            uiEntries[type] = texts[1];
        }
    }
}