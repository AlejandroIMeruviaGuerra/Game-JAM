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
    public Button yellowButton;
    public Button purpleButton;

    [Header("Game Settings")]
    public int maxTurns = 20;
    public int targetScore = 10000;

    [Header("Particles")]
    public GameObject redFX;
    public GameObject blueFX;
    public GameObject greenFX;
    public GameObject yellowFX;
    public GameObject purpleFX;

    [Header("Items Activos")]
    public List<EnergyItem> activeItems = new List<EnergyItem>();

    [Header("Visual Effects")]
    public EnergyVisualManager visualManager;

    [Header("Sistema de Rondas")]
    public int currentRound = 1;
    public float targetMultiplier = 1.3f; // cada ronda aumenta objetivo un 30%

    private int currentScore = 0;
    private int turnsLeft;
    private string lastEnergy = "";
    private int comboCount = 0;

    private Dictionary<string, EnergySynergy> synergies;

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
        yellowButton.onClick.AddListener(() => PlayTurn("Yellow"));
        purpleButton.onClick.AddListener(() => PlayTurn("Purple"));

    }

    void Awake()
    {
        InitializeSynergies();
    }

    void InitializeSynergies()
    {
        synergies = new Dictionary<string, EnergySynergy>()
    {
        { "Red-Green", new EnergySynergy("🔥 Fusión Vital", 2.0f, "Fuego y Naturaleza crean un impulso de vida.") },
        { "Green-Blue", new EnergySynergy("💧 Brote Marino", 1.8f, "Agua nutre la vida y potencia el siguiente turno.") },
        { "Blue-Red", new EnergySynergy("⚡ Tormenta Ígnea", 2.5f, "Electricidad calienta el aire y genera una tormenta.") },
        { "Yellow-Red", new EnergySynergy("☀️ Estallido Solar", 3.0f, "La luz enciende el fuego: daño masivo.") },
        { "Purple-Yellow", new EnergySynergy("🌑 Eclipse Total", 4.0f, "Luz y oscuridad chocan generando caos total.") },
    };
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

        }
        else
        {
            // 🔮 NUEVO SISTEMA DE SINERGIAS
            string key = lastEnergy + "-" + energyType;
            if (synergies.ContainsKey(key))
            {
                var synergy = synergies[key];
                basePoints = Mathf.RoundToInt(basePoints * synergy.multiplier);
                Debug.Log($"✨ {synergy.name}! ({synergy.description}) x{synergy.multiplier}");

                // Visual feedback (puedes personalizar)
                visualManager.TriggerCombo(comboCount + 1, Vector3.zero);
                EnergyAudioManager.Instance.PlayCombo(comboCount + 1);
            }

            comboCount = 1;
        }

        lastEnergy = energyType;
        float finalPoints = ApplyItemEffects(basePoints, energyType);

        currentScore += Mathf.RoundToInt(finalPoints);
        turnsLeft--;

        // 🔥 Nuevo: activa efecto visual
        visualManager.TriggerCombo(comboCount, Vector3.zero);

        EnergyAudioManager.Instance.PlayCombo(comboCount); // 🔥 Nuevo

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

            if (item.extendsCombo && comboCount > 3)
                points *= 1.2f;

            if (!string.IsNullOrEmpty(item.synergyBoost))
            {

                if (item.synergyBoost == lastEnergy + "-" + energyType)
                    points += 200;
            }

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
            var rewardPanel = FindObjectOfType<RewardPanelManager>(true);
            if (rewardPanel != null)
                rewardPanel.ShowRewardOptions();
            else
            {
                GiveRandomReward();
                NextRound();
            }

        }
        else
        {
            Debug.Log("💀 PERDISTE. Puntaje: " + currentScore);
        }
    }
    void NextRound()
    {
        currentRound++;
        targetScore = Mathf.RoundToInt(targetScore * targetMultiplier);
        maxTurns += 5; // más turnos si quieres más duración
        turnsLeft = maxTurns;
        currentScore = 0;
        comboCount = 0;

        Debug.Log("🌀 Ronda " + currentRound + " - Nuevo objetivo: " + targetScore);
        UpdateUI();
    }
    void SpawnFX(string type)
    {
        GameObject fx = null;
        if (type == "Red") fx = redFX;
        if (type == "Blue") fx = blueFX;
        if (type == "Green") fx = greenFX;
        if (type == "Yellow") fx = yellowFX;
        if (type == "Purple") fx = purpleFX;


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
[System.Serializable]
public class EnergySynergy
{
    public string name;
    public float multiplier;
    public string description;

    public EnergySynergy(string name, float multiplier, string description)
    {
        this.name = name;
        this.multiplier = multiplier;
        this.description = description;
    }
}
