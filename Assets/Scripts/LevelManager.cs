using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//***********************************************************************
//This is used to handle level and creattion of player and player history
//***********************************************************************
public class LevelManager : MonoBehaviour {

    [Header("Prefabs")]
    [SerializeField] private GameObject history_player_prefab;

    [SerializeField] private GameObject player_prefab;
    [SerializeField] private GameObject pickup_prefab;
    [SerializeField] private GameObject history_portal_prefab;
    [SerializeField] private GameObject player_portal_prefab;
    [SerializeField] private GameObject exit_portal_prefab;

    [Header("UI")]
    [SerializeField] private Slider incursion_meter;

    [SerializeField] private Text txtscore;
    [SerializeField] private Image whitescreen;

    [SerializeField] private List<Image> item_pickedup;

    [Header("LevelData")]
    [SerializeField] private GameData level_data;

    [SerializeField] private int spawn_wait;
    [SerializeField] private int incursions;
    [SerializeField] private int max_incursions;

    [Header("Point")]
    [SerializeField] private int pickup_points = 10;

    [SerializeField] private int exit_points = 50;

    [Header("Invulnerability")]
    [SerializeField] private int inv_seconds = 2;
    public Boolean invincible = false;

    [Header("Timer")]
    [SerializeField] private int start_time = 30;
    private int current_time;
    [SerializeField] private Text txtTimer ;
    private Coroutine timer_obj;



    private int current_item_spawn_point;
    private int items_collected;

    private GameObject exit_portal;
    private GameObject current_spawn_item;
    private GameObject player;
    private int score;

    public List<GameObject> history_players = new List<GameObject>();

    private SpawnPointList spawn_points;

    private IEnumerator MakeInvincible() {
        invincible = true;
        yield return new WaitForSeconds(inv_seconds);
        invincible = false;
    }

    private void Start() {
        SetWhiteToTransparent();
        spawn_points = GetComponent<SpawnPointList>();
        level_data.current_level = 0;
        StartCoroutine(Wait_And_Spawn_New_Player(0));
        //Spawn_New_Player(0);
        IncPickupScore(0);
        ResetSpawns();
        SetupIncursionMetere();
        ResetItemCollected();
        FadeOut();
    }

    private void SetWhiteToTransparent() {
        if (whitescreen != null) {
            whitescreen.canvasRenderer.SetAlpha(1.0f);
        }
    }

    private void FadeIn() {
        if (whitescreen != null) {
            whitescreen.CrossFadeAlpha(1.0f, 1.0f, true);
        }
    }

    private void FadeOut() {
        if (whitescreen != null) {
            whitescreen.CrossFadeAlpha(0.0f, 1.0f, true);
        }
    }

    public void ResetItemCollected() {
        foreach (Image e in item_pickedup) {
            e.enabled = false;
        }
        items_collected = 0;
    }

    public void IncItemCollected() {
        items_collected++;
        item_pickedup[items_collected].enabled = true;
    }

    public void TransportTo(GameObject go, Target target) {
        if (target.xpos_only == true) {
            go.transform.position = new Vector3(target.target.position.x, go.transform.position.y, go.transform.position.z);
        } else {
            go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y, go.transform.position.z);
        }
    }

    private void IncPickupScore(int points) {
        if (txtscore != null) {
            score = score + points;
            txtscore.text = "Score: " + score.ToString();
        }
    }

    private void IncIncursion() {
        if (incursion_meter != null) {
            incursions++;
            incursion_meter.value = incursions;
        }
        StartCoroutine(MakeInvincible());
    }

    private void ResetIncursion() {
        if (incursion_meter != null) {
            incursions = 0;
            incursion_meter.value = incursions;
        }
    }

    private void SetupIncursionMetere() {
        if (incursion_meter != null) {
            incursion_meter.maxValue = max_incursions;
            incursion_meter.value = incursions;
        }
    }

    private void ResetSpawns() {
        current_item_spawn_point = spawn_points.spawn_point_list.Count() - 1;
        Spawn_Spawn_Pickup_Item(current_item_spawn_point);
    }

    public void CloseStream(int level) {
        if (level_data.session_count[level] <= 0) {
            level_data.session_count[level] = level_data.session_count[level] - 1;
            // Debug.Log("Closing FS " + level);
            level_data.fs[level].Close();
            level_data.fs[level] = null;
        }
    }

    private void OnApplicationQuit() {
        CloseAllStreams();
    }

    public void Collition_With_History_Player() {
        if (invincible == false) {
            IncIncursion();
            if (incursions >= max_incursions) {
                //*********************
                Player_Died();
            }
        }
    }

    public void Player_Died() {
        //*********************
        //Destroy History Items
        //*********************
        StopAllCoroutines();
        foreach (GameObject go in history_players) {
            Destroy(go);
        }
        if (current_spawn_item != null) { Destroy(current_spawn_item); }
        Destroy(player);
        CloseExitPortal();
        history_players.Clear();
        level_data.current_level = 0;
        ResetIncursion();
        CloseAllStreams();
        ResetSpawns();
        //StartCoroutine(Wait_And_Spawn_New_Player(0));
        //Spawn_New_Player(0);
        ResetItemCollected();
        StartCoroutine(Fadout_to_endgame());
    }
    private IEnumerator Fadout_to_endgame() {
        FadeIn();
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene("EndGame");
    }

    private void Start_Timer() {
        Stop_Timer();
         current_time = start_time;
        timer_obj = StartCoroutine(Timer());
    }
    private void Stop_Timer() {
        if (timer_obj != null) {
            StopCoroutine(timer_obj);
        }
        txtTimer.text = "Time: " + start_time.ToString();
    }
    private IEnumerator Timer() {
        do {
            txtTimer.text ="Time: "  + current_time.ToString();
            yield return new WaitForSeconds(1);
            current_time--;  
        } while (current_time>0);
        txtTimer.text = "Time: " + current_time.ToString();
        Player_Died();

    }
    public void CloseAllStreams() {
        foreach (FileStream fs in level_data.fs) {
            if (fs != null) {
                fs.Close();
            }
        }
        level_data.fs.Clear();
        level_data.session_count.Clear();
    }

    public FileStream GetFileStream(int level, bool read) {
        FileStream fs = null;
        string path = Application.persistentDataPath + "/level" + level + ".json";
        bool need_to_add = true;
        if (level_data.fs.Count >= level + 1) {
            // Debug.Log("Retreaving FS " + level);
            fs = level_data.fs[level];
            need_to_add = false;
        }
        if (fs == null) {
            if (read == false) {
                //Debug.Log("Create FS " + level);
                fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            } else {
                //Debug.Log("open FS " + level);
                fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            if (need_to_add == true) {
                level_data.fs.Add(fs);
                level_data.session_count.Add(1);
            } else {
                level_data.fs[level] = fs;
                level_data.session_count[level] = level_data.session_count[level] + 1;
            }
        } else {
            level_data.session_count[level] = level_data.session_count[level] + 1;
            // Debug.Log("Retreaving FS " + level);
        }
        return fs;
    }

    private void SetSpawnPointRed(int level) {
        SpriteRenderer sr = spawn_points.spawn_point_list[level].GetComponent<SpriteRenderer>();
        sr.color = Color.red;
    }

    public void SetSpawnPointGreen(int level) {
        SpriteRenderer sr = spawn_points.spawn_point_list[level].GetComponent<SpriteRenderer>();
        sr.color = Color.green;
    }

    public void SetGameObjectToPos(GameObject go, int level) {
        if (spawn_points == null) { spawn_points = GetComponent<SpawnPointList>(); }
        go.transform.position = spawn_points.spawn_point_list[level].transform.position;
    }

    public void Spawn_Spawn_Pickup_Item(int level = 0) {
        current_spawn_item = Instantiate(pickup_prefab, spawn_points.spawn_point_list[level].transform.position, Quaternion.identity);
        Game2DTrigger trig = current_spawn_item.GetComponent<Game2DTrigger>();
        trig.level_manager = GetComponent<LevelManager>();
    }

    public GameObject Spawn_Entry_Portal(GameObject portal_prefab, int level = 0) {
        current_spawn_item = Instantiate(portal_prefab, spawn_points.spawn_point_list[level].transform.position, Quaternion.identity);
        return current_spawn_item;
    }

    public GameObject Spawn_Exit_Portal(int level = 0) {
        if (exit_portal != null) { Destroy(exit_portal); }
        exit_portal = Instantiate(exit_portal_prefab, spawn_points.spawn_point_list[level].transform.position, Quaternion.identity);
        Animator spawn_portal_a = exit_portal.GetComponent<Animator>();
        spawn_portal_a.SetInteger("state", 1);
        Game2DTrigger trig = exit_portal.GetComponent<Game2DTrigger>();
        trig.level_manager = GetComponent<LevelManager>();
        return exit_portal;
    }

    public void CloseExitPortal() {
        if (exit_portal != null) { Destroy(exit_portal); exit_portal = null; }
    }

    private IEnumerator Wait_And_Spawn_New_History_Player(int level = 0) {
        this.SetSpawnPointRed(level);
        GameObject spawn_portal = Spawn_Entry_Portal(history_portal_prefab, level);//Spawn a portal
        yield return new WaitForSeconds(spawn_wait);
        this.SetSpawnPointGreen(level);
        Animator spawn_portal_a = spawn_portal.GetComponent<Animator>();
        spawn_portal_a.SetInteger("state", 1);//Make it grow
        yield return new WaitForSeconds(1);
        Spawn_New_History_Player(level);
        Destroy(spawn_portal);
    }

    private IEnumerator Wait_And_Spawn_New_Player(int level = 0) {
        this.SetSpawnPointRed(level);
        GameObject spawn_portal = Spawn_Entry_Portal(player_portal_prefab, level);//Spawn a portal
        yield return new WaitForSeconds(spawn_wait);
        this.SetSpawnPointGreen(level);
        Animator spawn_portal_a = spawn_portal.GetComponent<Animator>();
        spawn_portal_a.SetInteger("state", 1);//Make it grow
        yield return new WaitForSeconds(1);
        Spawn_New_Player(level);
        Destroy(spawn_portal);
    }

    public void Spawn_New_Player(int level) {
        GameObject new_player = Instantiate(player_prefab, spawn_points.spawn_point_list[level].transform.position, Quaternion.identity);
        PlayerMovement pm = new_player.GetComponent<PlayerMovement>();
        player = new_player;
        pm.current_level = level;
        pm.is_history_player = false;
        pm.level_manager = GetComponent<LevelManager>();
        pm.fs = GetFileStream(level, false);
        StartCoroutine(MakeInvincible());
        current_time = start_time;
        Start_Timer();
    }

    public void Spawn_New_History_Player(int level = 0) {
        GameObject new_player = Instantiate(history_player_prefab, spawn_points.spawn_point_list[level].transform.position, Quaternion.identity);
        history_players.Add(new_player);
        PlayerMovement pm = new_player.GetComponent<PlayerMovement>();
        pm.current_level = level;
        pm.is_history_player = true;
        pm.level_manager = GetComponent<LevelManager>();
        Game2DTrigger trig = new_player.GetComponent<Game2DTrigger>();
        trig.level_manager = pm.level_manager;
        pm.fs = GetFileStream(level, true);
    }

    public void ItemCollected(GameObject go) {
        this.Spawn_Exit_Portal(11);
        IncPickupScore(pickup_points);
        IncItemCollected();
        Destroy(go);
    }

    public void EndGoal(GameObject go) {
        if (go != null) {
            PlayerMovement pm = go.GetComponent<PlayerMovement>();
            if (pm.is_history_player == false) {
                //******************
                //This is the player
                //******************
                Stop_Timer();
                CloseStream(level_data.current_level);
                Destroy(go);
                CloseExitPortal();
                level_data.IncLevel();
               
                //this.SetGameObjectToPos(go, level_data.current_level);
                //pm.current_level = level_data.current_level;
                //pm.fs = GetFileStream(pm.current_level, false);
                //pm.StartRecord();
                StartCoroutine(Wait_And_Spawn_New_Player(level_data.current_level));
                if (level_data.current_level > 0) {
                    StartCoroutine(Wait_And_Spawn_New_History_Player(0));
                }
                current_item_spawn_point--;
                IncPickupScore(exit_points);
                Spawn_Spawn_Pickup_Item(current_item_spawn_point);
         
            } else {
                int tmp_level = pm.current_level + 1;
                CloseStream(pm.current_level);
                this.history_players.Remove(player);
                Destroy(go);
                StartCoroutine(Wait_And_Spawn_New_History_Player(tmp_level));
            }
        }
    }
}