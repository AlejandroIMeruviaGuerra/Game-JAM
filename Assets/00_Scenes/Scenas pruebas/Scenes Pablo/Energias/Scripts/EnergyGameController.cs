using DG.Tweening;
using System.Collections;
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

    [Header("Energy Core Visual")]
    public EnergyCoreController coreController;

    [Header("Energy Effects")]
    public EnergyEffectManager energyEffectManager;


    [Header("UI Extra")]
    public TextMeshProUGUI roundBannerText;

    public AnimatedScore scoreAnimator;

    public List<RoundModifier> possibleModifiers;
    private RoundModifier activeModifier;

    private int currentScore = 0;
    private int turnsLeft;
    private string lastEnergy = "";
    private int comboCount = 0;

    private bool combosBlocked = false;
    private float synergyBoostMultiplier = 1f;

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

        // -----------------------------------------
        // COMBOS
        // -----------------------------------------
        // ⚠️ BORRAR ESTO EN EL FUTURO (solo para testear efectos visuales)
        if (energyType == lastEnergy)
        {
            comboCount++;
            basePoints *= comboCount;
        }

        // Sinergias (solo si no se bloquean combos)
        // Sinergias (solo si no se bloquean combos)
        if (!string.IsNullOrEmpty(lastEnergy) && !combosBlocked)
        {
            string key = lastEnergy + "-" + energyType;
            if (synergies.ContainsKey(key))
            {
                var synergy = synergies[key];
                basePoints = Mathf.RoundToInt(basePoints * (synergy.multiplier * synergyBoostMultiplier));

                Debug.Log($"✨ {synergy.name}! ({synergy.description}) x{synergy.multiplier * synergyBoostMultiplier}");

                // 🎨 Elegir color base según tipo actual (para el núcleo)
                Color synergyColor = Color.white;
                switch (energyType)
                {
                    case "Red": synergyColor = new Color(1f, 0.3f, 0.1f); break;
                    case "Blue": synergyColor = new Color(0.2f, 0.6f, 1f); break;
                    case "Green": synergyColor = new Color(0.1f, 1f, 0.4f); break;
                    case "Yellow": synergyColor = new Color(1f, 0.9f, 0.2f); break;
                    case "Purple": synergyColor = new Color(0.6f, 0.2f, 0.8f); break;
                }

                // 💫 Reacción especial del núcleo en sinergias
                if (coreController != null)
                {
                    coreController.SetEnergyColor(
                        new Color(synergyColor.r * 1.2f, synergyColor.g * 1.2f, synergyColor.b * 1.2f),
                        Color.white
                    );
                    coreController.StartCharge();
                    coreController.ActivatePulse(2.5f);
                    StartCoroutine(ReleaseCoreAfterDelay());
                }

                // Visual feedback para sinergia
                visualManager.TriggerCombo(comboCount + 1, Vector3.zero);
                EnergyAudioManager.Instance.PlayCombo(comboCount + 1);
            }

            FindObjectOfType<EnergySynergyVisuals>()?.ShowSynergy(key);
            FindObjectOfType<EnergyInfoPanel>()?.PopulatePanel(synergies, targetScore, maxTurns);

        }

        IEnumerator ReleaseCoreAfterDelay()
        {
            yield return new WaitForSeconds(2f);
            if (coreController != null)
                coreController.ReleaseCharge();
        }


        lastEnergy = energyType;

        // -----------------------------------------
        // MODIFICADOR DE RONDA
        // -----------------------------------------
        if (activeModifier != null)
        {
            float mult = 1f;
            switch (energyType)
            {
                case "Red": mult = activeModifier.redMultiplier; break;
                case "Blue": mult = activeModifier.blueMultiplier; break;
                case "Green": mult = activeModifier.greenMultiplier; break;
                case "Yellow": mult = activeModifier.yellowMultiplier; break;
                case "Purple": mult = activeModifier.purpleMultiplier; break;
            }

            basePoints = Mathf.RoundToInt(basePoints * mult);
        }

        // -----------------------------------------
        // OBJETOS ACTIVOS
        // -----------------------------------------
        float finalPoints = ApplyItemEffects(basePoints, energyType);

        currentScore += Mathf.RoundToInt(finalPoints);
        turnsLeft--;


        visualManager.TriggerCombo(comboCount, Vector3.zero);
        EnergyAudioManager.Instance.PlayCombo(comboCount);
        // Después de calcular comboCount
        FindObjectOfType<EnergyMomentumManager>()?.OnComboChanged(comboCount);

        SpawnFX(energyType);

        UpdateUI();

        // Fin de ronda
        if (turnsLeft <= 0)
            EndGame();
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

            // Bono por tipo de energía
            if (item.bonusEnergyType == energyType)
                extraFlat += item.bonusEnergyPoints;

            // Extiende combos
            if (item.extendsCombo && comboCount > 3)
                points *= 1.2f;

            // Sinergia potenciada
            if (!string.IsNullOrEmpty(item.synergyBoost) &&
                item.synergyBoost == lastEnergy + "-" + energyType)
            {
                points += 200;
            }

            // 🌀 NUEVO: regenerar turno al alcanzar combo alto
            if (item.regenerateTurn && comboCount >= 4)
            {
                AddExtraTurn();
                Debug.Log($"🔋 {item.itemName} te otorgó un turno adicional.");
            }

            // 🌀 NUEVO: conversión de energía (ejemplo: Amarillo → Rojo)
            if (item.convertEnergy && energyType == "Yellow")
            {
                energyType = "Red";
                Debug.Log($"🌈 {item.itemName} convirtió energía Amarilla en Roja.");
            }
        }

        return (points * totalMultiplier) + extraFlat;
    }


    void UpdateUI()
    {
        // Animar el texto de score (contador progresivo)
        int displayedScore = 0;

        // Si ya había un texto anterior, lo leemos (opcional)
        int.TryParse(scoreText.text.Replace("Score: ", ""), out displayedScore);

        // Cancelar animaciones previas (por seguridad)
        DOTween.Kill(scoreText);

        // Animar del valor actual mostrado al nuevo valor real
        DOTween.To(() => displayedScore, x =>
        {
            displayedScore = x;
            scoreText.text = "Score: " + displayedScore.ToString("N0"); // "N0" = formato con miles
        },
        currentScore, // valor objetivo
        0.5f // duración de la animación
        ).SetEase(Ease.OutQuad);
        // Pequeño rebote visual en el texto del score
        scoreText.transform.DOKill();
        scoreText.transform.localScale = Vector3.one;
        scoreText.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack)
            .OnComplete(() => scoreText.transform.DOScale(1f, 0.3f));

        // Flash de color temporal (a tono con la última energía usada)
        scoreText.DOColor(new Color(1f, 0.85f, 0.2f), 0.1f)
            .OnComplete(() => scoreText.DOColor(Color.white, 0.3f));

        // Actualizar los turnos sin animar (solo texto)
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
    public void NextRound()
    {
        Debug.Log("✅ Entrando a NextRound()");

        currentRound++;
        targetScore = Mathf.RoundToInt(targetScore * targetMultiplier);
        maxTurns += 5;
        turnsLeft = maxTurns;
        currentScore = 0;
        comboCount = 0;

        // Reiniciar efectos previos
        combosBlocked = false;
        synergyBoostMultiplier = 1f;
        FindObjectOfType<EnergyMomentumManager>()?.ResetMomentum();
        // Seleccionar modificador aleatorio
        if (possibleModifiers != null && possibleModifiers.Count > 0)
        {
            activeModifier = possibleModifiers[Random.Range(0, possibleModifiers.Count)];
            activeModifier.Apply(this);
            ShowRoundBanner(activeModifier);

            // 🌈 Cambiar color de fondo (con transición suave)
            if (Camera.main != null)
                StartCoroutine(AnimateBackgroundColor(activeModifier.bannerColor, 1.5f));
        }

        Debug.Log($"🌀 Ronda {currentRound} — Nuevo objetivo: {targetScore}");
        UpdateUI();
    }
    System.Collections.IEnumerator AnimateBackgroundColor(Color targetColor, float duration)
    {
        if (Camera.main == null) yield break;

        Color startColor = Camera.main.backgroundColor;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            Camera.main.backgroundColor = Color.Lerp(startColor, targetColor, t / duration);
            yield return null;
        }
    }


    void ShowRoundBanner(RoundModifier modifier)
    {
        if (roundBannerText == null)
        {
            Debug.LogWarning("⚠️ No se asignó roundBannerText en el Inspector.");
            return;
        }

        roundBannerText.text = $"{modifier.modifierName}\n<size=20>{modifier.description}</size>";
        roundBannerText.color = modifier.bannerColor;
        StartCoroutine(FadeBanner(roundBannerText, 0.8f));
    }

    System.Collections.IEnumerator FadeBanner(TextMeshProUGUI banner, float duration)
    {
        CanvasGroup group = banner.GetComponent<CanvasGroup>() ?? banner.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0;
        banner.gameObject.SetActive(true);

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(0, 1, t / duration);
            yield return null;
        }

        yield return new WaitForSeconds(2.5f);

        t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(1, 0, t / duration);
            yield return null;
        }

        banner.gameObject.SetActive(false);
    }



    System.Collections.IEnumerator HideBannerAfterDelay(TextMeshProUGUI banner, float delay)
    {
        yield return new WaitForSeconds(delay);
        banner.gameObject.SetActive(false);
    }

    void SpawnFX(string type)
    {
        GameObject fx = null;
        Color energyColor = Color.white;
        AudioClip sfx = null;

        switch (type)
        {
            case "Red":
                fx = redFX; energyColor = new Color(1f, 0.3f, 0.1f); sfx = EnergyAudioManager.Instance.clickSound; break;
            case "Blue":
                fx = blueFX; energyColor = new Color(0.2f, 0.6f, 1f); sfx = EnergyAudioManager.Instance.comboSound; break;
            case "Green":
                fx = greenFX; energyColor = new Color(0.1f, 1f, 0.4f); sfx = EnergyAudioManager.Instance.clickSound; break;
            case "Yellow":
                fx = yellowFX; energyColor = new Color(1f, 0.9f, 0.2f); sfx = EnergyAudioManager.Instance.megaComboSound; break;
            case "Purple":
                fx = purpleFX; energyColor = new Color(0.6f, 0.2f, 0.8f); sfx = EnergyAudioManager.Instance.comboSound; break;
        }

        if (fx != null)
            Instantiate(fx, Vector3.zero, Quaternion.identity);

        // 🔮 Reacción del núcleo según energía
        if (coreController != null)
        {
            coreController.SetEnergyColor(energyColor, energyColor * 1.8f);
            coreController.ActivatePulse(1.3f);
        }

        // 🌈 Fondo dinámico
        StartCoroutine(AnimateBackgroundColor(energyColor, 0.8f));

        // 💥 Score flash
        StartCoroutine(FlashScoreColor(energyColor));

        // 🔊 Sonido
        if (sfx != null)
            EnergyAudioManager.Instance.sfxSource.PlayOneShot(sfx);

        // 💫 Cámara shake leve
        StartCoroutine(ShakeCamera(0.15f, 0.08f));

        // 🎇 Actualizar backgroundFX
        visualManager.backgroundFX.SetBaseColor(energyColor);
    }

    IEnumerator FlashScoreColor(Color c)
    {
        var original = scoreText.color;
        scoreText.color = c;
        yield return new WaitForSeconds(0.2f);
        scoreText.color = original;
    }

    IEnumerator ShakeCamera(float duration, float intensity)
    {
        var cam = Camera.main;
        if (cam == null) yield break;
        Vector3 startPos = cam.transform.position;

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime;
            cam.transform.position = startPos + Random.insideUnitSphere * intensity;
            yield return null;
        }

        cam.transform.position = startPos;
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
    public void AddExtraTurn()
    {
        turnsLeft += 1;
        Debug.Log("🔁 +1 turno extra por efecto de ronda");
        UpdateUI();
    }

    public void BlockCombos(bool state)
    {
        combosBlocked = state;
        Debug.Log(state ? "🚫 Combos bloqueados esta ronda" : "✅ Combos activados nuevamente");
    }

    public void BoostSynergies(float factor)
    {
        synergyBoostMultiplier = factor;
        Debug.Log($"⚡ Potenciando sinergias x{factor}");
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
