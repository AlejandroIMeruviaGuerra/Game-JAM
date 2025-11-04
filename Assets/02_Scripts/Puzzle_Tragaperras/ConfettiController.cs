using UnityEngine;
using System.Collections;

public class ConfettiController : MonoBehaviour
{
    private ParticleSystem confetti;

    void Awake()
    {
        // Si ya existe uno, reutilizar (evita duplicados)
        GameObject existing = GameObject.Find("ConfettiFX");
        if (existing != null)
        {
            confetti = existing.GetComponent<ParticleSystem>();
            return;
        }

        // Crear el GameObject del confeti
        GameObject confettiGO = new GameObject("ConfettiFX");
        confettiGO.transform.SetParent(transform);
        confettiGO.transform.localPosition = new Vector3(0, 3f, 0);
        confettiGO.transform.localRotation = Quaternion.Euler(-259.5f, 0, 0);
        confettiGO.transform.localScale = new Vector3(3.5f, 1f, 1f);

        confetti = confettiGO.AddComponent<ParticleSystem>();
        var main = confetti.main;
        main.playOnAwake = false;
        confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var renderer = confetti.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 5;

        // --- CONFIGURACIÓN PRINCIPAL ---
        main.duration = 10f;
        main.loop = true; // ♾️ lluvia infinita
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2f);
        main.gravityModifier = 0.3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1200;

        // --- COLORES VARIADOS ---
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1f, 0.2f, 0.3f), 0.0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.2f),
                new GradientColorKey(new Color(0.3f, 0.8f, 1f), 0.4f),
                new GradientColorKey(new Color(0.3f, 1f, 0.5f), 0.6f),
                new GradientColorKey(new Color(0.9f, 0.3f, 1f), 0.8f),
                new GradientColorKey(Color.white, 1.0f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        var colorOverLifetime = confetti.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

        // --- EMISIÓN CONTINUA ---
        var emission = confetti.emission;
        emission.rateOverTime = 150f; // constante
        emission.burstCount = 0;

        // --- FORMA DE EMISIÓN ---
        var shape = confetti.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(12f, 0.5f, 2f);
        shape.position = Vector3.zero;

        // --- VELOCIDAD (modo consistente) ---
        var velocity = confetti.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(0f, 0f); // mantenemos constante
        velocity.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);

        // --- ROTACIÓN ---
        var rotation = confetti.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-360f, 360f);

        // --- TAMAÑO EN EL TIEMPO ---
        var size = confetti.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.5f);
        curve.AddKey(0.4f, 1f);
        curve.AddKey(1f, 0.3f);
        size.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // --- MATERIAL ---
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = Color.white;
        renderer.material.renderQueue = 3000;

        confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // 🔸 Iniciar confeti
    public void PlayConfetti()
    {
        if (confetti == null)
        {
            Debug.LogWarning("⚠️ No se encontró el sistema de confeti.");
            return;
        }

        confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        confetti.Play();
    }

    // 🔸 Detener confeti (por si lo deseas manual)
    public void StopConfetti()
    {
        if (confetti != null)
            confetti.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
