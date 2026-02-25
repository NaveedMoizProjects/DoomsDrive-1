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

        // Loop through the 4 types you defined in the Enum
        foreach (DamageablePart.PartType t in System.Enum.GetValues(typeof(DamageablePart.PartType)))
        {
            if (!uiEntries.ContainsKey(t)) CreateNewRow(t);

            // Filter registry by Type
            var partsOfType = DamageManager.Instance.carHealthRegistry.Values
                .Where(x => x.type == t);

            if (partsOfType.Any())
            {
                float avg = partsOfType.Average(x => x.health);
                UpdateRowVisuals(t, avg);
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