using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float transitionTime = 1f;
    private Animator transitionAnimator;
    void Start()
    {
        transitionAnimator = GetComponentInChildren<Animator>();

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKeyDown) 
        {
            LoadNextScene();
        }
     
        
    }
    void LoadNextScene() 
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        StartCoroutine(SceneLoad(nextSceneIndex));
    }
    public IEnumerator SceneLoad(int sceneIndex) 
    {
        //Disparar el trigger para reproducir animacion FadeIn
        transitionAnimator.SetTrigger("StartTransition");
        //Esperar uns egundo
        yield return new WaitForSeconds(transitionTime);
        //Cargar la siguiente escena
        SceneManager.LoadScene(sceneIndex);
    }

}
