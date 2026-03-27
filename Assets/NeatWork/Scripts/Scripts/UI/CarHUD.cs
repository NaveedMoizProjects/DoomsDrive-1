using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CarHUD : MonoBehaviour
{
    public Transform container;
    public GameObject rowPrefab;
    private Dictionary<DamageablePart.PartType, TextMeshProUGUI> uiEntries = new Dictionary<DamageablePart.PartType, TextMeshProUGUI>();

    void Update()
    {
        if (DamageManager.Instance == null) return;

        // Respect hudPaused flag (freeze HUD while respawn prompt / pause is active)
        if (DamageManager.Instance.hudPaused) return;

        foreach (DamageablePart.PartType t in System.Enum.GetValues(typeof(DamageablePart.PartType)))
        {
            if (!uiEntries.ContainsKey(t)) CreateNewRow(t);

            // Filter the Manager's registry for Player-owned parts of this type
            var playerParts = DamageManager.Instance.carHealthRegistry.Values
                .Where(x => x.type == t && x.ownerCar != null && x.ownerCar.CompareTag("Player"))
                .ToList();

            if (playerParts.Any())
            {
                // Average health for this category (e.g., Average of all 4 wheels)
                float avgHealth = playerParts.Average(x => x.health);
                UpdateRowVisuals(t, avgHealth);
            }
        }
    }

    void UpdateRowVisuals(DamageablePart.PartType type, float health)
    {
        if (!uiEntries.ContainsKey(type)) return;

        var text = uiEntries[type];
        text.text = $"{Mathf.CeilToInt(health)}%";

        // Dynamic Color
        if (health < 25) text.color = Color.red;
        else if (health < 60) text.color = Color.yellow;
        else text.color = Color.white;
    }

    void CreateNewRow(DamageablePart.PartType type)
    {
        GameObject newRow = Instantiate(rowPrefab, container);
        TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 2)
        {
            texts[0].text = type.ToString().ToUpper();
            uiEntries.Add(type, texts[1]);
        }
    }
}