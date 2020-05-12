using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuOptions : MonoBehaviour
{
    public void PlayGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

    }

    public void MainMenu() {
        SceneManager.LoadScene("MainMenu");

    }
    public void PlayAgain() {
        SceneManager.LoadScene("Zone1");

    }
    public void QuitGame() {
        Application.Quit();
    }
}
