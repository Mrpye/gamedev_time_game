using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentManagerScript : MonoBehaviour
{
    public static PersistentManagerScript Instance { get; private set; }
    public int score;
    public int lives;
    private void Awake() {
        if(Instance == null) {
            Instance = this;
            ResetScore();
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public bool AddLife() {
        lives++;
        if (lives > 3) {
            lives = 3;
            return true;
        }
        return false;
    }
    public void SubLife() {
        lives--;
        if (lives<0) {
         //   lives = 0;
        }
    }
    public void ResetLives() {
        lives = 3;
    }
    public void ResetScore() {
        score = 0;
        lives = 3;
    }
}
