using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RewardPanelManager : MonoBehaviour
{
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;
    private List<EnergyItem> offeredItems = new List<EnergyItem>();
    private MetaProgressionManager meta;
    private EnergyGameController controller;

    void Start()
    {
        meta = MetaProgressionManager.Instance;
        //controller = FindObjectOfType<EnergyGameController>();

        foreach (var btn in optionButtons)
            btn.onClick.AddListener(() => ChooseReward(btn));

        gameObject.SetActive(false);
    }

    public void ShowRewardOptions()
    {
        gameObject.SetActive(true);
        offeredItems.Clear();

        var available = meta.allItems.FindAll(i => !meta.unlockedItemNames.Contains(i.itemName));
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (available.Count == 0) break;

            var item = available[Random.Range(0, available.Count)];

            // 🎲 Generar rareza aleatoria aquí
            float roll = Random.value;
            if (roll < 0.6f) item.rarity = EnergyItem.ItemRarity.Common;
            else if (roll < 0.85f) item.rarity = EnergyItem.ItemRarity.Rare;
            else if (roll < 0.97f) item.rarity = EnergyItem.ItemRarity.Epic;
            else item.rarity = EnergyItem.ItemRarity.Legendary;

            offeredItems.Add(item);

            optionTexts[i].text = $"{item.itemName}\n<size=22><color=#{GetColorForRarity(item.rarity)}>{item.rarity}</color></size>";
            available.Remove(item);
        }
    }
    string GetColorForRarity(EnergyItem.ItemRarity rarity)
    {
        switch (rarity)
        {
            case EnergyItem.ItemRarity.Common: return "FFFFFF"; // blanco
            case EnergyItem.ItemRarity.Rare: return "00BFFF"; // azul
            case EnergyItem.ItemRarity.Epic: return "9932CC"; // morado
            case EnergyItem.ItemRarity.Legendary: return "FFD700"; // dorado
            default: return "FFFFFF";
        }
    }


    void ChooseReward(Button chosen)
    {
        int index = System.Array.IndexOf(optionButtons, chosen);
        if (index >= 0 && index < offeredItems.Count)
        {
            var item = offeredItems[index];
            meta.UnlockItem(item);

            // 🔁 Buscar dinámicamente el controlador cada vez (seguro)
            var controller = FindObjectOfType<EnergyGameController>();
            if (controller != null)
            {
                //controller.NextRound();
            }
            else
            {
                Debug.LogWarning("EnergyGameController no encontrado.");
            }
        }

        gameObject.SetActive(false);
    }

}
