using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnergyItemPanelUI : MonoBehaviour
{
    public List<Image> itemSlots;

    void Start()
    {
        RefreshIcons();
    }

    public void RefreshIcons()
    {
        var meta = MetaProgressionManager.Instance;
        if (meta == null) return;

        var equipped = meta.equippedItems;

        for (int i = 0; i < itemSlots.Count; i++)
        {
            if (i < equipped.Count && equipped[i].icon != null)
            {
                itemSlots[i].sprite = equipped[i].icon;
                itemSlots[i].color = Color.white;
            }
            else
            {
                itemSlots[i].sprite = null;
                itemSlots[i].color = new Color(1, 1, 1, 0.25f);
            }
        }
    }
}
