using UnityEngine;

public class LoopAnimation : MonoBehaviour
{
    [Header("Configuración de movimiento")]
    public float speed = 20f;       // Velocidad de movimiento hacia arriba
    public float upperLimit = 8f;    // Hasta dónde suben antes de reiniciar
    public float lowerLimit = -4f;   // Desde dónde reaparecen al reiniciar

    [Header("Configuración de copias")]
    public int copies = 10;           // Número de imágenes en la columna
    public float spacing = 2f;       // Espacio entre cada imagen

    private GameObject[] sprites;

    void Start()
    {
        // Crear arreglo para las copias
        sprites = new GameObject[copies];

        // Crear copias del sprite original una debajo de otra
        SpriteRenderer originalSR = GetComponent<SpriteRenderer>();

        for (int i = 0; i < copies; i++)
        {
            GameObject copy = new GameObject("Copy_" + i);
            copy.transform.parent = transform;
            copy.transform.localPosition = new Vector3(0, -i * spacing, 0);

            SpriteRenderer sr = copy.AddComponent<SpriteRenderer>();
            sr.sprite = originalSR.sprite;
            sr.sortingOrder = originalSR.sortingOrder;
            sr.color = originalSR.color;

            sprites[i] = copy;
        }

        // Ocultar el sprite original
        originalSR.enabled = false;
    }

    void Update()
    {
        foreach (var s in sprites)
        {
            s.transform.localPosition += Vector3.up * speed * Time.deltaTime;

            // Si la imagen supera el límite superior, reaparece abajo
            if (s.transform.localPosition.y > upperLimit)
            {
                s.transform.localPosition = new Vector3(0, lowerLimit, 0);
            }
        }
    }
}
