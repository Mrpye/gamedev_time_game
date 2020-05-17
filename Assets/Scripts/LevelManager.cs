using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//***********************************************************************
//This is used to handle level and creattion of player and player history
//***********************************************************************
public class LevelManager : MonoBehaviour {

    [Serializable]
    public struct LevelData {
        public int start;
        public int pickup;
        public int exit;

        public LevelData(int start, int pickup, int exit) {
            this.start = start;
            this.pickup = pickup;
            this.exit = exit;
        }
    }

    [Header("Debug")]
    [SerializeField] private bool cheat;

   
    [Header("Level Data")]
    [SerializeField] private List<LevelData> level_data = new List<LevelData>();


    [SerializeField] private int spawn_wait;


    [Header("Prefabs")]
    [SerializeField] private GameObject history_player_prefab;

    [SerializeField] private GameObject player_prefab;
    [SerializeField] private GameObject pickup_prefab;
    [SerializeField] private GameObject history_portal_prefab;
    [SerializeField] private GameObject player_portal_prefab;
    [SerializeField] private GameObject exit_portal_prefab;
    [SerializeField] private GameObject incursion_fix_prefab;
    [SerializeField] private GameObject ingame_menu;

    [Header("UI")]
    [SerializeField] private Slider incursion_meter;
    [SerializeField] private Text txtscore;
    [SerializeField] private Image whitescreen;
    [SerializeField] private List<Image> item_pickedup;
    [SerializeField] private List<Image> item_lives;
    [SerializeField] private Text txtTimer;

    [Header("Point")]
    [SerializeField] private int pickup_points = 10;
    [SerializeField] private int exit_points = 50;

    [Header("Invulnerability")]
    [SerializeField] private int inv_seconds = 2;
    [SerializeField] private int time_freeze_seconds = 5;
    public Boolean invincible = false;

    [Header("Incursion")]
    [SerializeField] private int incursions;
    [SerializeField] private int max_incursions;
    [SerializeField] int Incursion_Fix_at = 5;
    [SerializeField] int Incursion_Fix_at_location = 5;
    [NonSerialized] private bool incursion_fix = false;


    [Header("Timer")]
    [SerializeField] private int start_time = 30;
    private int current_time;
    private Coroutine timer_obj;
    public bool time_frozen;

    [Header("Audio")]
    [SerializeField] public AudioClip time_freeze_audio;
    [SerializeField] public AudioClip crystal_used_audio;
    AudioSource audioSource;


    private int items_collected;
    public int current_level;
    private GameObject exit_portal;
    private GameObject current_spawn_item;
    private GameObject incursion_fix_item;
    private GameObject player;
    private PersistentManagerScript score;
    private SpawnPointList spawn_points;
    [NonSerialized] public List<GameObject> history_players = new List<GameObject>();
    [NonSerialized] public List<FileStream> fs = new List<FileStream>();
    [NonSerialized] public List<int> session_count = new List<int>();

    private void Start() {
        CleanUp();
        score = GameObject.Find("PersistentScoreManager").GetComponent<PersistentManagerScript>();
        SetWhiteToTransparent();
        spawn_points = GetComponent<SpawnPointList>();
        current_level = 0;
        StartCoroutine(Wait_And_Spawn_New_Player(0));
        //Spawn_New_Player(0);
        IncPickupScore(0);
        Spawn_Spawn_Pickup_Item(0);
        SetupIncursionMetere();
        ResetItemCollected();
        FadeOut();
        ResetLifes();
        incursion_fix = false;
        time_frozen = false;
        audioSource = GetComponent<AudioSource>();
    }

    public void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)){
            if(ingame_menu != null) {
                ingame_menu.SetActive(true);
               
            }
        }
    }


    public void CleanUpAllElements() {
        //*********************
        //Destroy History Items
        //*********************
        StopAllCoroutines();
        foreach (GameObject go in history_players) {
            Destroy(go);
        }
        if (current_spawn_item != null) { Destroy(current_spawn_item); }
        if (exit_portal != null) { Destroy(exit_portal); }
        if (current_spawn_item != null) { Destroy(current_spawn_item); }
        if (incursion_fix_item != null) { Destroy(incursion_fix_item); }
        Destroy(player);
        CloseExitPortal();
        history_players.Clear();
        current_level = 0;
        ResetIncursion();
        CloseAllStreams();
        Spawn_Spawn_Pickup_Item(current_level);
        ResetItemCollected();
    }


    public void IncLevel() {
        current_level++;
    }

    private IEnumerator MakeInvincible() {
        invincible = true;
        yield return new WaitForSeconds(inv_seconds);
        invincible = false;
    }

    private IEnumerator FreezeTime() {
        time_frozen = true;
        if (time_freeze_audio != null) { audioSource.PlayOneShot(time_freeze_audio, 0.7F); }
        yield return new WaitForSeconds(time_freeze_seconds);
        time_frozen = false;
    }

    private void SetWhiteToTransparent() {
        if (whitescreen != null) {
            whitescreen.canvasRenderer.SetAlpha(1.0f);
        }
    }
    public void TimeCrystalUsed() {
        if (time_frozen == false) { 
            DecLife();
            if (incursions == 0) {
                StartCoroutine(FreezeTime());
            } else {
               
                DecIncursion();
            }
        } else {
            if (incursions > 0) {
                DecLife();
                DecIncursion();
            }
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


    #region Lives
    public void ResetLifes() {
        for (int i = 0; i < this.item_lives.Count; i++) {
            item_lives[i].enabled = true;
        }
    }

    public void DecLife() {
        score.SubLife();
        for (int i = 0; i < this.item_lives.Count; i++) {
            if (i <= score.lives- 1) {
                item_lives[i].enabled = true;
            } else {
                item_lives[i].enabled = false;
            }
        }
    }

    public void AddLife() {
        if (score.AddLife()) {
            //We got a full set of lifes so time will get frozen
            StartCoroutine(FreezeTime());
        }
        for (int i = 0; i < this.item_lives.Count; i++) {
            if (i <= score.lives - 1) {
                item_lives[i].enabled = true;
            } else {
                item_lives[i].enabled = false;
            }
        }
    }

    #endregion


    #region TimeCrystal Functions
    public void DecItemCollected() {
        items_collected = current_level;
        for (int i = 0; i < item_pickedup.Count; i++) {
            if (i >= items_collected) {
                item_pickedup[i].enabled = false;
            } else {
                item_pickedup[i].enabled = true;
            }
        }
    }

    public void ResetItemCollected() {
        foreach (Image e in item_pickedup) {
            e.enabled = false;
        }
        items_collected = 0;
    }

    public void IncItemCollected() {
        item_pickedup[items_collected].enabled = true;
        items_collected++;
    }
    #endregion


    public void TransportTo(GameObject go, Target target) {
        if (target.xpos_only == true) {
            go.transform.position = new Vector3(target.target.position.x, go.transform.position.y, go.transform.position.z);
        } else {
            go.transform.position = new Vector3(target.target.transform.position.x, target.target.transform.position.y, go.transform.position.z);
        }
    }

    private void IncPickupScore(int points) {
        if (txtscore != null) {
            score.score = score.score + points;
            txtscore.text = "Score: " + score.score.ToString();
        }
    }

    #region Incursions Functions
    private void DecIncursion() {
        incursions--;
        if (incursion_meter != null) {
            if (crystal_used_audio != null) { audioSource.PlayOneShot(crystal_used_audio, 0.7F); }
            if (incursions < 0) { incursions = 0; }
            incursion_meter.value = incursions;
        }
        StartCoroutine(MakeInvincible());
    }

    private void IncIncursion() {
        if (incursion_meter != null) {
            if (cheat == false) {
                incursions++;
            }
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
    #endregion


    public void CloseStream(int level) {
        if (session_count[level] <= 0) {
            session_count[level] = session_count[level] - 1;
            // Debug.Log("Closing FS " + level);
            this.fs[level].Close();
            this.fs[level] = null;
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
                SpaceTimeDestroyed();
            }
        }
    }

    public void Player_Died() {
        DecLife();
        current_level--;
        if (current_level < 0) { current_level = 0; }
        if (score.lives < 0) {
            SpaceTimeDestroyed();
        } else {
            //*********************
            //Destroy History Items
            //*********************
            StopAllCoroutines();
            foreach (GameObject go in history_players) {
                Destroy(go);
            }
            CleanUp();

            Destroy(player);
            CloseExitPortal();
            history_players.Clear();
            
            ResetIncursion();
            CloseAllStreams();
            Spawn_Spawn_Pickup_Item(current_level);
            //ResetItemCollected();
            DecItemCollected();
            for(int i=0; i< current_level; i++) {
                StartCoroutine(Wait_And_Spawn_New_History_Player(i));
            }
            StartCoroutine(Wait_And_Spawn_New_Player(current_level));
        }
    }

   
    public void Player_Diedold() {
        CloseStream(current_level);
        current_level--;
        if (current_level < 0) {
            SpaceTimeDestroyed();
        } else {
            List<GameObject> tmp = new List<GameObject>();
            foreach (GameObject go in history_players) {
                if (go != null) {
                    PlayerMovement pm = go.GetComponent<PlayerMovement>();
                    if (pm.current_level >= current_level) {
                        Destroy(go);
                    } else {
                        if (pm.current_level == current_level - 1) {
                            pm.current_pos = 0;
                        }
                        tmp.Add(go);
                    }
                }
            }
            history_players = tmp;
            CleanUp();
            Destroy(player);
            CloseExitPortal();
            DecItemCollected();
            CloseStream(current_level);
            StartCoroutine(Wait_And_Spawn_New_Player(current_level));
            Spawn_Spawn_Pickup_Item(current_level);
        }
    }

    

    public void SpaceTimeDestroyed() {
        CleanUpAllElements();
        StartCoroutine(Fadout_to_endgame());
    }

    private IEnumerator Fadout_to_endgame() {
        
        FadeIn();
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene("EndGame");
    }
    private IEnumerator Fadout_to_NextLevel() {
        FadeIn();
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
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
            txtTimer.text = "Time: " + current_time.ToString();
            yield return new WaitForSeconds(1);
            current_time--;
        } while (current_time > 0);
        txtTimer.text = "Time: " + current_time.ToString();
        SpaceTimeDestroyed();
    }



    #region FileManager

    public void CloseAllStreams() {
        foreach (FileStream fs in this.fs) {
            if (fs != null) {
                fs.Close();
            }
        }
        this.fs.Clear();
        session_count.Clear();
    }
    private void CleanUp() {
        if (current_spawn_item != null) { Destroy(current_spawn_item); }
        if (exit_portal != null) { Destroy(exit_portal); }
        if (current_spawn_item != null) { Destroy(current_spawn_item); }
        if (incursion_fix_item != null) { Destroy(incursion_fix_item); }
    }
    public FileStream GetFileStream(int level, bool read) {
        FileStream fs = null;
        string path = Application.persistentDataPath + "/level" + level + ".json";
        bool need_to_add = true;
        if (this.fs.Count >= level + 1) {
            // Debug.Log("Retreaving FS " + level);
            fs = this.fs[level];
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
                this.fs.Add(fs);
                session_count.Add(1);
            } else {
                this.fs[level] = fs;
                session_count[level] = session_count[level] + 1;
            }
        } else {
            session_count[level] = session_count[level] + 1;
            // Debug.Log("Retreaving FS " + level);
        }
        return fs;
    }
    #endregion




    private void SetSpawnPointRed(int level) {
        SpriteRenderer sr = spawn_points.spawn_point_list[level_data[level].start].GetComponent<SpriteRenderer>();
        sr.color = Color.red;
    }

    public void SetSpawnPointGreen(int level) {
        SpriteRenderer sr = spawn_points.spawn_point_list[level_data[level].start].GetComponent<SpriteRenderer>();
        sr.color = Color.green;
    }

    public void SetGameObjectToPos(GameObject go, int level) {
        if (spawn_points == null) { spawn_points = GetComponent<SpawnPointList>(); }
        go.transform.position = spawn_points.spawn_point_list[level_data[level].start].transform.position;
    }

    public void Spawn_Spawn_Pickup_Item(int level = 0) {
        current_spawn_item = Instantiate(pickup_prefab, spawn_points.spawn_point_list[level_data[level].pickup].transform.position, Quaternion.identity);
        SpriteRenderer sr = current_spawn_item.GetComponent<SpriteRenderer>();
        sr.sprite = item_pickedup[level].sprite;
        sr.color = item_pickedup[level].color;
        Game2DTrigger trig = current_spawn_item.GetComponent<Game2DTrigger>();
        trig.level_manager = GetComponent<LevelManager>();
    }

    public GameObject Spawn_Entry_Portal(GameObject portal_prefab, int level = 0) {
        current_spawn_item = Instantiate(portal_prefab, spawn_points.spawn_point_list[level_data[level].start].transform.position, Quaternion.identity);
        return current_spawn_item;
    }

    public GameObject Spawn_Exit_Portal(int level = 0) {
        if (exit_portal != null) { Destroy(exit_portal); }
        exit_portal = Instantiate(exit_portal_prefab, spawn_points.spawn_point_list[level_data[level].exit].transform.position, Quaternion.identity);
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
        if (spawn_portal != null) {
            Animator spawn_portal_a = spawn_portal.GetComponent<Animator>();
            if (spawn_portal_a != null) { spawn_portal_a.SetInteger("state", 1); }
        }
        yield return new WaitForSeconds(1);
        Spawn_New_History_Player(level);
        if (spawn_portal != null) { Destroy(spawn_portal); }
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
        GameObject new_player = Instantiate(player_prefab, spawn_points.spawn_point_list[level_data[level].start].transform.position, Quaternion.identity);
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
        GameObject new_player = Instantiate(history_player_prefab, spawn_points.spawn_point_list[level_data[level].start].transform.position, Quaternion.identity);
        history_players.Add(new_player);
        PlayerMovement pm = new_player.GetComponent<PlayerMovement>();
        pm.current_level = level;
        pm.is_history_player = true;
        pm.level_manager = GetComponent<LevelManager>();
        Game2DTrigger trig = new_player.GetComponent<Game2DTrigger>();
        trig.level_manager = pm.level_manager;
        pm.fs = GetFileStream(level, true);
    }


    private IEnumerator Wait_Spawn_New_Incursion_Fix() {
        GameObject spawn_portal = Instantiate(player_portal_prefab, spawn_points.spawn_point_list[Incursion_Fix_at_location].transform.position, Quaternion.identity);
        yield return new WaitForSeconds(spawn_wait);
        Animator spawn_portal_a = spawn_portal.GetComponent<Animator>();
        spawn_portal_a.SetInteger("state", 1);//Make it grow
        yield return new WaitForSeconds(1);
        Spawn_New_Incursion_Fix(Incursion_Fix_at_location);
        Destroy(spawn_portal);
    }

    public void Spawn_New_Incursion_Fix(int Incursion_Fix_at_location) {
        GameObject new_player = Instantiate(incursion_fix_prefab, spawn_points.spawn_point_list[Incursion_Fix_at_location].transform.position, Quaternion.identity);
        Game2DTrigger trig = new_player.GetComponent<Game2DTrigger>();
        trig.level_manager =this;

    }

    public void Time_Crystal_Collected(GameObject go) {
        AddLife();
        Destroy(go);
    }

    public void ItemCollected(GameObject go) {
        this.Spawn_Exit_Portal(current_level);
        IncPickupScore(pickup_points);
        IncItemCollected();
        if(items_collected >= Incursion_Fix_at && incursion_fix == false) {
            incursion_fix = true;
            StartCoroutine(Wait_Spawn_New_Incursion_Fix());
        }
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
                CloseStream(current_level);
                Destroy(go);
                CloseExitPortal();
                IncLevel();
                if (cheat && current_level >= 1) {
                    CleanUpAllElements();
                    StartCoroutine(Fadout_to_NextLevel());
                }else if (current_level >= level_data.Count) {
                    CleanUpAllElements();
                    StartCoroutine(Fadout_to_NextLevel());
                } else {
                    StartCoroutine(Wait_And_Spawn_New_Player(current_level));
                    if (current_level > 0) {
                        StartCoroutine(Wait_And_Spawn_New_History_Player(0));
                    }
                    //current_item_spawn_point--;
                    IncPickupScore(exit_points);
                    Spawn_Spawn_Pickup_Item(current_level);
                }
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