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

        foreach (DamageablePart.PartType t in System.Enum.GetValues(typeof(DamageablePart.PartType)))
        {
            if (!uiEntries.ContainsKey(t)) CreateNewRow(t);

            // FIX: Filter registry to ONLY include parts belonging to the Player's car
            var playerPartsOfType = DamageManager.Instance.carHealthRegistry.Values
                .Where(x => x.type == t && x.ownerCar.CompareTag("Player")); // Check for Player tag

            if (playerPartsOfType.Any())
            {
                float avg = playerPartsOfType.Average(x => x.health);
                UpdateRowVisuals(t, avg);
            }
            else
            {
                // If no player parts exist for this type (e.g. all doors fell off), show 0
                UpdateRowVisuals(t, 0f);
            }
        }
    }

    void UpdateRowVisuals(DamageablePart.PartType type, float health)
    {
        var text = uiEntries[type];
        text.text = $"{Mathf.CeilToInt(health)}";

        // Color based on health
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
            // Use the Enum name as the Label (Wheel, Door, Body, Core)
            texts[0].text = type.ToString();
            uiEntries.Add(type, texts[1]);
        }
    }
}