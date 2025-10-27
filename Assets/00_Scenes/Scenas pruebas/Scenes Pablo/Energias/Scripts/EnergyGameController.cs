using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnergyGameController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI turnsText;
    public Button redButton;
    public Button blueButton;
    public Button greenButton;

    [Header("Game Settings")]
    public int maxTurns = 20;
    public int targetScore = 10000;

    [Header("Particles")]
    public GameObject redFX;
    public GameObject blueFX;
    public GameObject greenFX;

    [Header("Items Activos")]
    public List<EnergyItem> activeItems = new List<EnergyItem>();

    [Header("Visual Effects")]
    public EnergyVisualManager visualManager;


    private int currentScore = 0;
    private int turnsLeft;
    private string lastEnergy = "";
    private int comboCount = 0;

    void Start()
    {
        EnergyAudioManager.Instance.PlayClick();
        turnsLeft = maxTurns;
        activeItems = MetaProgressionManager.Instance.equippedItems;
        UpdateUI();

        // Asignar listeners
        redButton.onClick.AddListener(() => PlayTurn("Red"));
        blueButton.onClick.AddListener(() => PlayTurn("Blue"));
        greenButton.onClick.AddListener(() => PlayTurn("Green"));
    }

    void PlayTurn(string energyType)
    {
        if (turnsLeft <= 0) return;

        int basePoints = Random.Range(100, 300);


        // Combo check
        if (energyType == lastEnergy)
        {
            comboCount++;
            basePoints *= comboCount; // multiplicador creciente


            // 🔥 Nuevo: activa efecto visual
            visualManager.TriggerCombo(comboCount, Vector3.zero);

            EnergyAudioManager.Instance.PlayCombo(comboCount); // 🔥 Nuevo
        }
        else
        {
            comboCount = 1;
        }

        lastEnergy = energyType;
        float finalPoints = ApplyItemEffects(basePoints, energyType);

        currentScore += Mathf.RoundToInt(finalPoints);
        turnsLeft--;

        SpawnFX(energyType);
        UpdateUI();

        if (turnsLeft == 0)
        {
            EndGame();
        }
    }


    float ApplyItemEffects(float points, string energyType)
    {
        float totalMultiplier = 1f;
        int extraFlat = 0;

        foreach (var item in activeItems)
        {
            totalMultiplier *= item.scoreMultiplier;
            points *= item.comboMultiplier;
            extraFlat += item.flatBonus;

            // Si el objeto da bonus por tipo de energía
            if (item.bonusEnergyType == energyType)
                extraFlat += item.bonusEnergyPoints;
        }

        return (points * totalMultiplier) + extraFlat;
    }

    void UpdateUI()
    {
        scoreText.text = "Score: " + currentScore;
        turnsText.text = "Turns Left: " + turnsLeft;
    }

    void EndGame()
    {
        if (currentScore >= targetScore)
        {
            Debug.Log("🎉 GANASTE! Puntaje final: " + currentScore);

            // 🔹 Recompensa: desbloquear un objeto aleatorio
            GiveRandomReward();
        }
        else
        {
            Debug.Log("💀 PERDISTE. Puntaje: " + currentScore);
        }
    }
    void SpawnFX(string type)
    {
        GameObject fx = null;
        if (type == "Red") fx = redFX;
        if (type == "Blue") fx = blueFX;
        if (type == "Green") fx = greenFX;

        if (fx != null)
            Instantiate(fx, Vector3.zero, Quaternion.identity);
    }
    void GiveRandomReward()
    {
        var meta = MetaProgressionManager.Instance;
        var catalog = meta.allItems;

        // Buscar un objeto no desbloqueado aún
        var locked = catalog.FindAll(i => !meta.unlockedItemNames.Contains(i.itemName));

        if (locked.Count > 0)
        {
            int r = Random.Range(0, locked.Count);
            meta.UnlockItem(locked[r]);
        }
        else
        {
            Debug.Log("⭐ Ya tienes todos los objetos desbloqueados.");
        }
    }
}
