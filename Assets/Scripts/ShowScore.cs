using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShowScore : MonoBehaviour
{
    [SerializeField] PersistentManagerScript score;
    // Start is called before the first frame update
    void Start()
    {
       // if (score == null) {
            score = GameObject.Find("PersistentScoreManager").GetComponent<PersistentManagerScript>();
       // }
      
        Text txt = GetComponent<Text>();

        
        if (score != null && txt != null) {
            txt.text = "Score: " + score.score.ToString();
        }
        

    }

    
}
