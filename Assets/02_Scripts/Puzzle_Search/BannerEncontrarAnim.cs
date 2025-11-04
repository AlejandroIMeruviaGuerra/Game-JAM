using UnityEngine;
using UnityEngine.UI;

public class BannerEncontrarAnim : MonoBehaviour
{
    [Header("Referencias")]
    public RectTransform fondo;      // Fondo del banner
    public RectTransform letras;     // Imagen de letras "¡ENCONTRAR!"

    [Header("Configuración de movimiento")]
    public float velocidadScroll = 100f;  // Velocidad del movimiento del fondo
    public float duracionVisible = 2.5f;  // Tiempo en pantalla antes de desaparecer
    public float tiempoFade = 0.6f;       // Tiempo para desvanecerse

    private float tiempo;
    private Image imgFondo;
    private Image imgLetras;
    private bool ocultando = false;

    void Start()
    {
        imgFondo = fondo.GetComponent<Image>();
        imgLetras = letras.GetComponent<Image>();

        tiempo = 0f;
    }

    void Update()
    {
        // Deslizar el fondo (scroll horizontal infinito)
        Vector2 pos = fondo.anchoredPosition;
        pos.x += velocidadScroll * Time.deltaTime;
        if (pos.x > 512) pos.x = -512; // reset para loop
        fondo.anchoredPosition = pos;

        // Cronómetro
        tiempo += Time.deltaTime;

        // Desaparecer con fade
        if (tiempo >= duracionVisible && !ocultando)
        {
            ocultando = true;
            StartCoroutine(FadeOut());
        }
    }

    System.Collections.IEnumerator FadeOut()
    {
        float t = 0f;
        Color cFondo = imgFondo.color;
        Color cLetras = imgLetras.color;

        while (t < tiempoFade)
        {
            float alpha = Mathf.Lerp(1f, 0f, t / tiempoFade);
            imgFondo.color = new Color(cFondo.r, cFondo.g, cFondo.b, alpha);
            imgLetras.color = new Color(cLetras.r, cLetras.g, cLetras.b, alpha);
            t += Time.deltaTime;
            yield return null;
        }

        // Desactivar
        fondo.gameObject.SetActive(false);
        letras.gameObject.SetActive(false);
    }
}
