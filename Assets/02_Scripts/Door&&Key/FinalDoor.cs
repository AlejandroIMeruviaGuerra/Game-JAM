using UnityEngine;
using System.Collections;

public class FinalDoor : Door
{
    [Header("Configuración Puerta Final")]
    public GameObject victoryPanel;
    public string nextSceneName;

    protected override IEnumerator OpenDoor()
    {
        // Respeta la lógica base (incluye setear trigger y animación)
        yield return base.OpenDoor();

        // Extra final
        OnFinalDoorOpened();
    }

    private void OnFinalDoorOpened()
    {
        Debug.Log("🎉 ¡VICTORIA! Has completado el nivel.");

        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        if (!string.IsNullOrEmpty(nextSceneName))
            Invoke(nameof(LoadNextScene), 3f);
    }

    private void LoadNextScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
}
