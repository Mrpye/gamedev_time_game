using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] GameObject PlayerPrefab;
    [SerializeField] GameData level_data;
    private void Start() {
        level_data.CurrentLevel = 0;
        //Vector3 start = level_data.GetNextStartPoint();
        //GameObject prefab = Instantiate(PlayerPrefab, start,Quaternion.identity);



    }
}
