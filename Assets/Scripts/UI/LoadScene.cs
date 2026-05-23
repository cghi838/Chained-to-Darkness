using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    // Loads the scene using sceneName
    public void Load(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Loads the scene using sceneIndex
    public void Load(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
