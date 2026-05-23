using UnityEngine;
using UnityEngine.InputSystem;

public class GameOverSceneManager : MonoBehaviour
{
    public string sceneName = "GameScene 1";

    void Update()
    {
        if (InputSystem.actions["Jump"].WasPressedThisFrame())
        {
            GameManager.Instance.NewGame(); // Ensure timer is running for new game
            Debug.Log("GameOverSceneManager: Jump pressed, restarting game...");
            // Restart the game by loading the first scene
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
}
