using UnityEngine;

[CreateAssetMenu(fileName = "NewEnergyItem", menuName = "EnergyGame/Energy Item")]
public class EnergyItem : ScriptableObject
{
    [Header("Información del objeto")]
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Efecto del objeto")]
    public float scoreMultiplier = 1f;        // Multiplica todos los puntos
    public float comboMultiplier = 1f;        // Multiplica la ganancia por combo
    public int flatBonus = 0;                 // Suma puntos fijos por turno
    public string bonusEnergyType = "";       // Por ejemplo "Red", "Blue"...
    public int bonusEnergyPoints = 0;         // Si coincide el tipo, se añade
}
