using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RewardPanelManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public Button[] optionButtons;
    public TextMeshProUGUI[] optionTexts;

    private List<EnergyItem> offeredItems = new List<EnergyItem>();
    private MetaProgressionManager meta;
    private EnergyGameController controller;

    void Awake()
    {
        // 🔁 Buscar dinámicamente siempre (más seguro)
        meta = MetaProgressionManager.Instance ?? FindObjectOfType<MetaProgressionManager>(true);
        controller = FindObjectOfType<EnergyGameController>(true);

        if (meta == null)
            Debug.LogWarning("⚠️ RewardPanelManager no encontró MetaProgressionManager aún.");

        gameObject.SetActive(false);
    }

    void Start()
    {
        meta = MetaProgressionManager.Instance ?? FindObjectOfType<MetaProgressionManager>(true);
        controller = FindObjectOfType<EnergyGameController>(true);

        // 🔹 Asignar correctamente cada listener
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i; // importante para evitar bug del closure
            optionButtons[i].onClick.AddListener(() => ChooseReward(index));
        }

        gameObject.SetActive(false);
    }


    public void ShowRewardOptions()
    {
        // 🔒 Verificaciones seguras
        if (meta == null)
        {
            meta = MetaProgressionManager.Instance ?? FindObjectOfType<MetaProgressionManager>(true);
            if (meta == null)
            {
                Debug.LogError("❌ No se encontró MetaProgressionManager. Asegúrate de que está en la escena o con DontDestroyOnLoad.");
                return;
            }
        }

        if (meta.allItems == null || meta.allItems.Count == 0)
        {
            Debug.LogError("❌ meta.allItems está vacío o no asignado.");
            return;
        }

        gameObject.SetActive(true);
        offeredItems.Clear();

        // Filtrar ítems no desbloqueados
        var available = meta.allItems.FindAll(i => !meta.unlockedItemNames.Contains(i.itemName));

        if (available == null || available.Count == 0)
        {
            Debug.LogWarning("⭐ Ya tienes todos los objetos desbloqueados.");
            gameObject.SetActive(false);
            controller?.NextRound();
            return;
        }

        // Generar hasta 3 opciones
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (available.Count == 0) break;

            var item = available[Random.Range(0, available.Count)];

            // 🎲 Generar rareza aleatoria
            float roll = Random.value;
            if (roll < 0.6f) item.rarity = EnergyItem.ItemRarity.Common;
            else if (roll < 0.85f) item.rarity = EnergyItem.ItemRarity.Rare;
            else if (roll < 0.97f) item.rarity = EnergyItem.ItemRarity.Epic;
            else item.rarity = EnergyItem.ItemRarity.Legendary;

            offeredItems.Add(item);

            // Mostrar texto con color por rareza
            if (optionTexts != null && i < optionTexts.Length)
                optionTexts[i].text = $"{item.itemName}\n<size=22><color=#{GetColorForRarity(item.rarity)}>{item.rarity}</color></size>";

            available.Remove(item);
        }

        Debug.Log("🎁 Llamando ShowRewardOptions()");

        if (meta == null)
        {
            meta = MetaProgressionManager.Instance ?? FindObjectOfType<MetaProgressionManager>(true);
            if (meta == null)
            {
                Debug.LogError("❌ No se encontró MetaProgressionManager.");
                return;
            }
        }

        Debug.Log($"📦 meta.allItems.Count = {meta.allItems.Count}");
    }

    string GetColorForRarity(EnergyItem.ItemRarity rarity)
    {
        switch (rarity)
        {
            case EnergyItem.ItemRarity.Common: return "FFFFFF";
            case EnergyItem.ItemRarity.Rare: return "00BFFF";
            case EnergyItem.ItemRarity.Epic: return "9932CC";
            case EnergyItem.ItemRarity.Legendary: return "FFD700";
            default: return "FFFFFF";
        }
    }

    void ChooseReward(int index)
    {
        if (index >= 0 && index < offeredItems.Count)
        {
            var item = offeredItems[index];
            meta ??= MetaProgressionManager.Instance ?? FindObjectOfType<MetaProgressionManager>(true);

            if (meta != null)
                meta.UnlockItem(item);
            else
                Debug.LogError("❌ MetaProgressionManager sigue sin encontrarse al elegir recompensa.");

            controller ??= FindObjectOfType<EnergyGameController>(true);

            Debug.Log($"✅ Elegiste recompensa: {item.itemName}");
            controller?.NextRound();
        }

        gameObject.SetActive(false);
    }

}
