using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerLevelManager : MonoBehaviour {
    [SerializeField] private GameData levelinfo;
    [SerializeField] private SpawnPointList spawnpoints;
    [SerializeField] private GameObject playerprefab;
    [SerializeField] private List<GameObject> history_players = new List<GameObject>();
    public bool is_history_player;
    public int this_level;
    private PlayerMovement player_movment;

    private void Start() {
        player_movment = gameObject.GetComponent<PlayerMovement>();
        //Let setup other players
        if (is_history_player ==true) {
            SetUpPlayer(gameObject, this_level, true);
        } else {
            string path = Application.persistentDataPath + "/level" + this_level + ".json";
            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            levelinfo.fs.Add(fs);
            SetUpPlayer(gameObject, this_level , false);
        }
    }

    public void EndGoal() {
        if (this.is_history_player) {
            player_movment.Stop();
            this_level++;
            if (this_level > levelinfo.CurrentLevel) {
                Destroy(gameObject);
            } else {
                gameObject.transform.position = spawnpoints.spawnpoints[this_level].transform.position;
                player_movment.record_path = Application.persistentDataPath + "/level" + this_level + ".json";
                player_movment.fs = levelinfo.fs[this_level];
                player_movment.StartPlayBack();
            }
        } else {
            player_movment.Stop();
            levelinfo.IncLevel();
            this_level = levelinfo.CurrentLevel;
            gameObject.transform.position = spawnpoints.spawnpoints[this_level].transform.position;
            string path = Application.persistentDataPath + "/level" + levelinfo.CurrentLevel + ".json";
            player_movment.record_path = path;
            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            levelinfo.fs.Add(fs);

            player_movment.fs = fs;
            
        
              if (levelinfo.CurrentLevel > 0 ) { 
                GameObject new_player = Instantiate(playerprefab, spawnpoints.spawnpoints[0].transform.position, Quaternion.identity);
                PlayerLevelManager plm= new_player.GetComponent<PlayerLevelManager>();
                plm.this_level = 0;
                plm.is_history_player = true;
            }
            player_movment.StartRecord();
        }
    }

    private void SetUpPlayer(GameObject player, int level, bool playback) {
        string destination = Application.persistentDataPath + "/level" + level + ".json";
        player_movment.record_path = destination;
        player_movment.fs = levelinfo.fs[level];
        gameObject.transform.position = spawnpoints.spawnpoints[level].transform.position;
        PlayerLevelManager plm = player.GetComponent<PlayerLevelManager>();
        plm.is_history_player = playback;
        plm.this_level = level;
       
        if (playback == true) {
            player_movment.mode = PlayerMovement.Mode.PlayBack;
            player_movment.StartPlayBack();
        } else {
            player_movment.mode = PlayerMovement.Mode.Record;
            player_movment.StartRecord();
        }
    }
}