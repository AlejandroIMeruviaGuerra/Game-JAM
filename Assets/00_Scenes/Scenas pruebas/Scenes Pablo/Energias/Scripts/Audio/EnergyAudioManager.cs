using UnityEngine;

public class EnergyAudioManager : MonoBehaviour
{
    public static EnergyAudioManager Instance;

    [Header("Sonidos base")]
    public AudioSource sfxSource;
    public AudioClip clickSound;
    public AudioClip comboSound;
    public AudioClip megaComboSound;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    public void PlayClick()
    {
        if (clickSound != null)
            sfxSource.PlayOneShot(clickSound);
    }

    public void PlayCombo(int comboCount)
    {
        if (comboCount >= 10 && megaComboSound != null)
        {
            sfxSource.pitch = 1.3f;
            sfxSource.PlayOneShot(megaComboSound);
        }
        else if (comboSound != null)
        {
            sfxSource.pitch = Mathf.Lerp(1f, 1.2f, comboCount / 10f);
            sfxSource.PlayOneShot(comboSound);
        }
    }
}
