using UnityEngine;

[System.Serializable]
public class EnergyVisualProfile
{
    public string energyName; // "Red", "Blue", etc.
    public ParticleSystem fxPrefab;
    public Color glowColor;
    public AudioClip sfxClip;
    public Material overrideMaterial; // opcional: para shaders únicos
}

public class EnergyFXLibrary : MonoBehaviour
{
    public static EnergyFXLibrary Instance;
    public EnergyVisualProfile[] profiles;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public EnergyVisualProfile GetProfile(string energy)
    {
        foreach (var p in profiles)
            if (p.energyName == energy)
                return p;
        return null;
    }
}
