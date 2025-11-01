using UnityEngine;

[CreateAssetMenu(fileName = "NewEnergyItem", menuName = "Energias/Energy Item")]
public class EnergyItem : ScriptableObject
{
    [Header("Datos generales")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemRarity rarity = ItemRarity.Common;
    public int level = 1;

    [Header("Efectos base")]
    public float scoreMultiplier = 1f;
    public float comboMultiplier = 1f;
    public int flatBonus = 0;

    [Header("Bonificaciones de energía")]
    public string bonusEnergyType; // "Red", "Blue", etc.
    public int bonusEnergyPoints;

    [Header("Comportamiento especial")]
    public bool extendsCombo;
    public string synergyBoost; // ejemplo "Red-Green"
    public bool regenerateTurn; // nuevo: +1 turno al lograr combo alto
    public bool convertEnergy;  // nuevo: convierte un tipo de energía a otro

    [Header("Visual y feedback")]
    public Color auraColor = Color.white;

    public enum ItemRarity { Common, Rare, Epic, Legendary }
}
