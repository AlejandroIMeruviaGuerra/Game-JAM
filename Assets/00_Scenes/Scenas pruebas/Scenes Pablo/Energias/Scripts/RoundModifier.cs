using UnityEngine;

[CreateAssetMenu(fileName = "NewRoundModifier", menuName = "EnergyGame/Round Modifier")]
public class RoundModifier : ScriptableObject
{
    [Header("Información del modificador")]
    public string modifierName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Multiplicadores de Energía")]
    public float redMultiplier = 1f;
    public float blueMultiplier = 1f;
    public float greenMultiplier = 1f;
    public float yellowMultiplier = 1f;
    public float purpleMultiplier = 1f;

    [Header("Bonificaciones especiales")]
    public bool extraTurnOnSynergy;
    public float comboBonusMultiplier = 1f;

    public void Apply(EnergyGameController controller)
    {
        Debug.Log($"🔮 Nueva ronda: {modifierName} — {description}");
    }
}
