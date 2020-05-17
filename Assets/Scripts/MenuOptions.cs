using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuOptions : MonoBehaviour
{
    public void PlayGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

    }

    public void ResetScore() {
        PersistentManagerScript score = GameObject.Find("PersistentScoreManager").GetComponent<PersistentManagerScript>();
        score.ResetScore();
    }
    public void MainMenu() {
        ResetScore();
        SceneManager.LoadScene("MainMenu");

    }

    public void Instructions() {
        SceneManager.LoadScene("Instructions");
    }
    public void PlayAgain() {
        ResetScore();
        SceneManager.LoadScene("Zone1a");

    }
    public void LevelSelect() {
        ResetScore();
        SceneManager.LoadScene("LevelSelect");

    }
    public void Level1() {
        ResetScore();
        SceneManager.LoadScene("Zone1a");

    }
    public void Level2() {
        ResetScore();
        SceneManager.LoadScene("Zone1b");

    }
    public void Level3() {
        ResetScore();
        SceneManager.LoadScene("Zone1c");

    }
    public void QuitGame() {
        Application.Quit();
    }
}
