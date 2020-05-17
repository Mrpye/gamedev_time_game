using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMenu : MonoBehaviour
{
    
    private void OnEnable() {
        Time.timeScale = 0;
    }

    public void Return() {
        gameObject.SetActive(false);
        Resume();
    }
  
    public void Resume() {
        Time.timeScale = 1;
    } 



    public void ResetScore() {
        PersistentManagerScript score = GameObject.Find("PersistentScoreManager").GetComponent<PersistentManagerScript>();
        score.ResetScore();
    }
    public void MainMenu() {
        gameObject.SetActive(false);
        ResetScore();
        SceneManager.LoadScene("MainMenu");

    }
}
