using UnityEngine;
using TMPro;

public class SlotButtonF : MonoBehaviour
{
    [Header("Máquina")]
    public SlotMachineController controller;

    [Header("Entrada")]
    public KeyCode key = KeyCode.F;
    public float debounce = 0.25f;

    [Header("Prompt (opcional)")]
    public GameObject promptGO;          // arrastra CanvasTragaperras o el TMP
    public string promptText = "Presiona F";
    public bool faceCamera = true;       // que el prompt mire a la cámara

    [Header("Feedback (opcional)")]
    public Renderer rendererToTint;
    public Color hoverColor = new Color(0.2f, 0.8f, 1f, 1f);
    public Color idleColor = Color.white;
    public AudioSource sfx;
    public AudioClip sfxClick;

    bool inside = false;
    float lastUse = -999f;
    TMP_Text tmp;

    void Reset()
    {
        var rb = GetComponent<Rigidbody>(); if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false; rb.isKinematic = true;
        var col = GetComponent<Collider>(); if (col) col.isTrigger = true;
    }

    void Start()
    {
        if (rendererToTint && rendererToTint.material) rendererToTint.material.color = idleColor;
        if (promptGO)
        {
            tmp = promptGO.GetComponentInChildren<TMP_Text>(true);
            if (tmp && !string.IsNullOrEmpty(promptText)) tmp.text = promptText;
            promptGO.SetActive(false);
        }
    }

    void Update()
    {
        if (inside && Input.GetKeyDown(key) && Time.time - lastUse > debounce)
        {
            lastUse = Time.time;
            if (controller) controller.OnMainButton();
            if (sfx && sfxClick) sfx.PlayOneShot(sfxClick, 0.9f);
        }

        if (inside && faceCamera && promptGO && Camera.main)
        {
            // Billboarding simple
            promptGO.transform.forward = Camera.main.transform.forward;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        inside = true;
        if (rendererToTint && rendererToTint.material) rendererToTint.material.color = hoverColor;
        if (promptGO) promptGO.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        inside = false;
        if (rendererToTint && rendererToTint.material) rendererToTint.material.color = idleColor;
        if (promptGO) promptGO.SetActive(false);
    }

    bool IsPlayer(Collider c) => c.CompareTag("Player") || c.GetComponent<CharacterController>() != null;
}
