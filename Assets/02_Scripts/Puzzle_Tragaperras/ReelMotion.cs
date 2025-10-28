using UnityEngine;

public class ReelMotion : MonoBehaviour
{
    public float speed = 5f;
    public float spacing = 2f;
    public Sprite[] symbols;

    private SpriteRenderer[] renders;
    private float offset = 0f;

    void Awake()
    {
        renders = GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renders.Length; i++)
            renders[i].sprite = symbols[Random.Range(0, symbols.Length)];
    }

    void Update()
    {
        offset += Time.deltaTime * speed;

        // mueve cada sprite hacia abajo
        for (int i = 0; i < renders.Length; i++)
        {
            float y = -offset + i * spacing;
            renders[i].transform.localPosition = new Vector3(0, y, 0);
        }

        // reinicia cuando pasa el umbral
        if (offset > spacing)
        {
            offset -= spacing;
            // recicla el último sprite
            for (int i = renders.Length - 1; i > 0; i--)
                renders[i].sprite = renders[i - 1].sprite;

            renders[0].sprite = symbols[Random.Range(0, symbols.Length)];
        }
    }
}
