using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TMP_Text))]
public class FloatingWaveTextUI : MonoBehaviour
{
    [Header("Configuración del efecto")]
    public float amplitude = 6f;
    public float frequency = 2f;
    public float waveSpeed = 2f;
    public float letterSpacing = 0.3f;
    public bool autoStart = true;

    private TMP_Text tmpText;
    private TMP_TextInfo textInfo;
    private bool isAnimating;
    private float time;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        // Reinicia automáticamente al habilitar el objeto
        if (autoStart)
            StartCoroutine(RestartAfterFrame());
    }

    IEnumerator RestartAfterFrame()
    {
        yield return null; // espera 1 frame a que se renderice la UI
        StartWave();
    }

    public void StartWave()
    {
        if (isAnimating)
        {
            StopAllCoroutines();
            isAnimating = false;
        }

        StartCoroutine(AnimateWave());
    }

    IEnumerator AnimateWave()
    {
        isAnimating = true;
        tmpText.ForceMeshUpdate();

        while (true)
        {
            tmpText.ForceMeshUpdate();
            textInfo = tmpText.textInfo;
            time += Time.deltaTime * waveSpeed;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                int vertexIndex = textInfo.characterInfo[i].vertexIndex;
                int meshIndex = textInfo.characterInfo[i].materialReferenceIndex;
                Vector3[] vertices = textInfo.meshInfo[meshIndex].vertices;

                float wave = Mathf.Sin(time * frequency + i * letterSpacing) * amplitude;
                Vector3 offset = new Vector3(0, wave, 0);

                for (int j = 0; j < 4; j++)
                    vertices[vertexIndex + j] += offset;
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            yield return null;
        }
    }
}
