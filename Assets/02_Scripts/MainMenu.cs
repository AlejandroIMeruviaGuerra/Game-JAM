using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Llamado desde el botón Comenzar
    public void StartGame()
    {
        // Nombre exacto de tu escena
        SceneManager.LoadScene("SceneAle");
    }

    // Llamado desde el botón Salir
    public void QuitGame()
    {
        Debug.Log("Salir del juego");
        Application.Quit();
    }
}
