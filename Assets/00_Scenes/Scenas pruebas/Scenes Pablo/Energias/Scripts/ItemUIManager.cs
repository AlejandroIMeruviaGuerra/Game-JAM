using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ItemUIManager : MonoBehaviour
{
    public EnergyGameController gameController;
    public GameObject itemSlotPrefab;
    public Transform contentParent;

    void Start()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        foreach (Transform child in contentParent)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        foreach (var item in gameController.activeItems)
        {
            var slot = Instantiate(itemSlotPrefab, contentParent);
            slot.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = item.itemName;
            slot.transform.Find("Icon").GetComponent<Image>().sprite = item.icon;
        }
    }
}
