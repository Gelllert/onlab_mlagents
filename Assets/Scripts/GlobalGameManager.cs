using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalGameManager : MonoBehaviour
{
    public static GlobalGameManager Instance {get; private set;}

    [Header("Game setting")]
    public bool isTrainingMode = true;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void LoadNextLevel()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        LoadSceneByIndex(nextSceneIndex);
    }

    public void LoadSceneByIndex(int index)
    {
        if (index < 0 || index >= SceneManager.sceneCountInBuildSettings) return; 
        SceneManager.LoadScene(index);
    }
}
