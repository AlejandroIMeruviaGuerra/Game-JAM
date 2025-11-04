using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SceneMusic : MonoBehaviour
{
    [Header("Música")]
    public AudioClip music;               // Asigna tu clip (.wav/.mp3/.ogg)
    [Range(0f, 1f)] public float volume = 0.6f;
    public float fadeIn = 0.6f;           // seg
    public float fadeOut = 0.4f;          // seg
    public bool autoPlay = true;          // empieza solo al entrar a escena

    private AudioSource src;

    void Reset()
    {
        // Auto-ajustes útiles si añades el componente desde el inspector
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;            // 2D
    }

    void Awake()
    {
        src = GetComponent<AudioSource>();

        // Normalizamos el AudioSource SIEMPRE (aunque lo toques en el inspector)
        src.playOnAwake = false;
        src.loop = true;
        src.spatialBlend = 0f;            // 2D
        src.priority = 128;
        if (music != null) src.clip = music;

        src.volume = 0f;                  // arrancamos a 0 para el fade-in

        // Sanidad: avisos útiles
        if (FindObjectOfType<AudioListener>() == null)
            Debug.LogWarning("[SceneMusic] No hay AudioListener en escena (ponlo en la Main Camera).");
        if (src.clip == null)
            Debug.LogWarning("[SceneMusic] No hay AudioClip asignado.");
    }

    void OnEnable()
    {
        if (autoPlay) TryStart();
    }

    void Start()
    {
        // Por si OnEnable no corrió (des/activaciones raras)
        if (autoPlay && !src.isPlaying) TryStart();
    }

    private void TryStart()
    {
        if (src.clip == null) return;

        src.Stop();
        src.time = 0f;
        src.volume = 0f;
        src.Play();
        if (fadeIn > 0f) StartCoroutine(FadeTo(volume, fadeIn));
        else src.volume = volume;
    }

    public System.Collections.IEnumerator FadeAndStop()
    {
        yield return FadeTo(0f, fadeOut);
        src.Stop();
    }

    private System.Collections.IEnumerator FadeTo(float target, float time)
    {
        float start = src.volume;
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;  // el fade no depende del Time.timeScale
            src.volume = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        src.volume = target;
    }

    void OnDestroy()
    {
        if (src && src.isPlaying) src.Stop();
    }
}
