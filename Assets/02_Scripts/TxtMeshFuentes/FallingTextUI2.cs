using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TMP_Text))]
public class FallingTextUI2 : MonoBehaviour
{
    [Header("Configuración de animación")]
    public float letterDropDelay = 0.12f;
    public float dropDistance = 80f;
    public float dropDuration = 0.4f;
    public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 1.2f, 1, 0f);

    private TMP_Text tmpText;
    private TMP_TextInfo textInfo;
    private Vector3[][] initialVertices;
    private bool animating;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (animating) return;
        StartCoroutine(AnimateText());
    }

    IEnumerator AnimateText()
    {
        animating = true;

        // 👇 Usa el texto que ya tiene el TMP en la escena
        tmpText.ForceMeshUpdate();
        textInfo = tmpText.textInfo;

        if (textInfo.characterCount == 0)
        {
            animating = false;
            yield break;
        }

        // Guardar posición inicial de cada carácter visible
        initialVertices = new Vector3[textInfo.characterCount][];
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int meshIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            // Seguridad de rango
            if (meshIndex < 0 || meshIndex >= textInfo.meshInfo.Length)
                continue;

            Vector3[] verts = textInfo.meshInfo[meshIndex].vertices;
            if (verts == null || vertexIndex + 3 >= verts.Length)
                continue;

            initialVertices[i] = new Vector3[4];
            for (int j = 0; j < 4; j++)
                initialVertices[i][j] = verts[vertexIndex + j];
        }

        // Ocultar todas las letras al inicio
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var colors = textInfo.meshInfo[i].colors32;
            for (int j = 0; j < colors.Length; j++)
                colors[j].a = 0;
        }
        tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        yield return null;

        // Animar caída letra por letra
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            if (initialVertices[i] == null) continue;

            StartCoroutine(DropLetter(i));
            yield return new WaitForSeconds(letterDropDelay);
        }

        animating = false;
    }

    IEnumerator DropLetter(int index)
    {
        TMP_CharacterInfo charInfo = textInfo.characterInfo[index];
        if (!charInfo.isVisible) yield break;

        int meshIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        if (meshIndex >= textInfo.meshInfo.Length) yield break;
        if (initialVertices[index] == null) yield break;

        Vector3[] originalVerts = initialVertices[index];
        TMP_MeshInfo meshInfo = textInfo.meshInfo[meshIndex];
        Vector3[] vertices = meshInfo.vertices;
        Color32[] colors = meshInfo.colors32;

        if (vertexIndex + 3 >= vertices.Length || vertexIndex + 3 >= colors.Length)
            yield break;

        float t = 0f;
        float duration = dropDuration;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);
            float offset = fallCurve.Evaluate(progress) * dropDistance;

            // Mover las letras hacia abajo
            for (int j = 0; j < 4; j++)
                vertices[vertexIndex + j] = originalVerts[j] - Vector3.up * offset;

            // Aumentar la opacidad mientras cae
            byte alpha = (byte)(progress * 255);
            for (int j = 0; j < 4; j++)
                colors[vertexIndex + j].a = alpha;

            tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            tmpText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            yield return null;
        }
    }
}
