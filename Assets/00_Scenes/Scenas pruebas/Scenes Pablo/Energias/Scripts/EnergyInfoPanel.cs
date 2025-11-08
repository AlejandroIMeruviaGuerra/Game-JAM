using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnergyInfoPanel : MonoBehaviour
{
    [System.Serializable]
    public class SynergyUIRow
    {
        public Image iconA;
        public Image iconB;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI multiplierText;
    }

    [Header("Synergy Rows (assign in Inspector)")]
    public List<SynergyUIRow> rows;

    [Header("Energy Icons")]
    public Sprite redIcon, blueIcon, greenIcon, yellowIcon, purpleIcon;

    [Header("Extra Info")]
    public TextMeshProUGUI objectiveText;
    public TextMeshProUGUI turnsText;
    public TextMeshProUGUI tipText;

    private Dictionary<string, Sprite> iconMap;

    void Start()
    {
        iconMap = new Dictionary<string, Sprite>
        {
            { "Red", redIcon }, { "Blue", blueIcon },
            { "Green", greenIcon }, { "Yellow", yellowIcon },
            { "Purple", purpleIcon }
        };
    }

    public void PopulatePanel(Dictionary<string, EnergySynergy> synergies, int objective, int turns)
    {
        int index = 0;
        foreach (var kv in synergies)
        {
            if (index >= rows.Count) break;
            string[] parts = kv.Key.Split('-');
            if (parts.Length == 2)
            {
                rows[index].iconA.sprite = iconMap[parts[0]];
                rows[index].iconB.sprite = iconMap[parts[1]];
            }

            rows[index].nameText.text = kv.Value.name;
            rows[index].multiplierText.text = $"x{kv.Value.multiplier:F1}";
            index++;
        }

        objectiveText.text = $"🎯 Objetivo: {objective:N0}";
        turnsText.text = $"🔁 Turnos: {turns}";
        tipText.text = "💡 Combina energías para crear sinergias poderosas.";
    }
    public void UpdateObjective(int newObjective)
    {
        if (objectiveText != null)
            objectiveText.text = $"🎯 Objetivo: {newObjective:N0}";
    }

    public void UpdateTurns(int newTurns)
    {
        if (turnsText != null)
            turnsText.text = $"🔁 Turnos: {newTurns}";
    }

}
