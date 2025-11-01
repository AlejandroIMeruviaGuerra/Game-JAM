using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "NewRoundModifier", menuName = "Energias/Round Modifier")]
public class RoundModifier : ScriptableObject
{
    [Header("Identidad de la Ronda")]
    public string modifierName;
    [TextArea(2, 4)] public string description;
    public Color bannerColor = Color.white;

    [Header("Multiplicadores por Energía")]
    [Range(0.1f, 3f)] public float redMultiplier = 1f;
    [Range(0.1f, 3f)] public float blueMultiplier = 1f;
    [Range(0.1f, 3f)] public float greenMultiplier = 1f;
    [Range(0.1f, 3f)] public float yellowMultiplier = 1f;
    [Range(0.1f, 3f)] public float purpleMultiplier = 1f;

    [Header("Efectos adicionales (opcional futuro)")]
    public bool extraTurn;
    public bool blockCombos;
    public bool doubleSynergies;

    public void Apply(EnergyGameController controller)
    {
        Debug.Log($"🎯 Aplicando modificador: {modifierName}");
        if (extraTurn) controller.AddExtraTurn();
        if (blockCombos) controller.BlockCombos(true);
        if (doubleSynergies) controller.BoostSynergies(2f);
    }
}
