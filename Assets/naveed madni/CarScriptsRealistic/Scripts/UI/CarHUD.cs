using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class CarHUD : MonoBehaviour
{
    [Header("Setup")]
    public Transform container; // The UI Panel with a Vertical Layout Group
    public GameObject rowPrefab; // The DamageRow_Prefab you made

    // We store the created rows here so we can update them without flickering
    private Dictionary<string, TextMeshProUGUI> uiEntries = new Dictionary<string, TextMeshProUGUI>();

    void Update()
    {
        if (DamageManager.Instance == null) return;

        // Loop through every part registered in the Singleton
        foreach (var entry in DamageManager.Instance.carHealthRegistry)
        {
            string pName = entry.Key;
            float pHealth = entry.Value;

            // 1. If we don't have a UI row for this part yet, create one
            if (!uiEntries.ContainsKey(pName))
            {
                CreateNewRow(pName);
            }

            // 2. Update the damage number (Integer only)
            // We use :0 to remove decimals
            uiEntries[pName].text = Mathf.CeilToInt(pHealth).ToString();

            // SPECIAL: If it's the Core, make the text Bold or Yellow
            if (pName.ToLower().Contains("core"))
            {
                uiEntries[pName].fontWeight = FontWeight.Bold;
                if (pHealth < 25) uiEntries[pName].color = Color.red; // Critical warning
            }
            // 3. Visual Polish: Make it red if destroyed
            if (pHealth <= 0) uiEntries[pName].color = Color.red;
            else uiEntries[pName].color = Color.white;
        }
    }

    void CreateNewRow(string name)
    {
        GameObject newRow = Instantiate(rowPrefab, container);

        // Find the text components inside the prefab
        // Assuming PartName is index 0 and Value is index 1
        TextMeshProUGUI[] texts = newRow.GetComponentsInChildren<TextMeshProUGUI>();

        if (texts.Length >= 2)
        {
            texts[0].text = name; // Set the Part Name once
            uiEntries.Add(name, texts[1]); // Store the Value text for updates
        }
    }
}