using System.Collections.Generic;
using UnityEngine;

public class MetaProgressionManager : MonoBehaviour
{
    public static MetaProgressionManager Instance;

    [Header("Catálogo completo de objetos")]
    public List<EnergyItem> allItems = new List<EnergyItem>();

    [Header("Objetos desbloqueados")]
    public List<string> unlockedItemNames = new List<string>();

    [Header("Objetos equipados actualmente")]
    public List<EnergyItem> equippedItems = new List<EnergyItem>();

    private void Awake()
    {
        // Singleton simple
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }

        LoadProgress();
    }

    // 🔹 Agregar un objeto desbloqueado
    public void UnlockItem(EnergyItem item)
    {
        if (!unlockedItemNames.Contains(item.itemName))
        {
            unlockedItemNames.Add(item.itemName);
            SaveProgress();
            Debug.Log("🔓 Desbloqueaste: " + item.itemName);
        }
    }

    // 🔹 Equipar un objeto si está desbloqueado
    public void EquipItem(EnergyItem item)
    {
        if (unlockedItemNames.Contains(item.itemName) && !equippedItems.Contains(item))
        {
            equippedItems.Add(item);
        }
    }

    // 🔹 Desequipar un objeto
    public void UnequipItem(EnergyItem item)
    {
        if (equippedItems.Contains(item))
            equippedItems.Remove(item);
    }

    // 🔹 Guardar el progreso
    public void SaveProgress()
    {
        PlayerPrefs.SetString("UnlockedItems", string.Join(",", unlockedItemNames));
        PlayerPrefs.Save();
    }

    // 🔹 Cargar el progreso al inicio
    public void LoadProgress()
    {
        unlockedItemNames.Clear();
        equippedItems.Clear();

        if (PlayerPrefs.HasKey("UnlockedItems"))
        {
            string data = PlayerPrefs.GetString("UnlockedItems");
            unlockedItemNames.AddRange(data.Split(','));

            // Vincular objetos Scriptable según los nombres
            foreach (var item in allItems)
            {
                if (unlockedItemNames.Contains(item.itemName))
                    equippedItems.Add(item); // por ahora equipamos todos
            }
        }
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey("UnlockedItems");
        unlockedItemNames.Clear();
        equippedItems.Clear();
    }
}
