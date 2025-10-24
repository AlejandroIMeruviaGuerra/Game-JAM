using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipMenuManager : MonoBehaviour
{
    public MetaProgressionManager meta;
    public GameObject itemSlotPrefab;
    public Transform contentParent;

    void Start()
    {
        UpdateList();
    }

    public void UpdateList()
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (var item in meta.allItems)
        {
            var slot = Instantiate(itemSlotPrefab, contentParent);
            slot.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = item.itemName;
            slot.transform.Find("Icon").GetComponent<Image>().sprite = item.icon;

            Button equipButton = slot.transform.Find("EquipButton").GetComponent<Button>();
            var localItem = item;
            equipButton.onClick.AddListener(() => ToggleEquip(localItem));
        }
    }

    void ToggleEquip(EnergyItem item)
    {
        if (meta.equippedItems.Contains(item))
        {
            meta.UnequipItem(item);
            Debug.Log("❌ Desequipado: " + item.itemName);
        }
        else
        {
            meta.EquipItem(item);
            Debug.Log("✅ Equipado: " + item.itemName);
        }
    }
}
