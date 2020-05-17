using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    // private types
    [Serializable]
    public struct InputSequence        // var names are reduced for smaller json
    {
        public float t;         // time
        public float x;
        public float y;

        public void init() {
            x = 0;
            y = 0;
        }
    };

    public enum Mode { Record, PlayBack };

    private bool bGrounded = true;

    public enum UpdateFunction { FixedUpdate, Update, Both };

    private bool eof = false;
    public long current_pos = 0;//Store the file stream pointer

    public FileStream fs;
    private StreamReader inputPlaybackStream;
    private StreamWriter inputRecordStream;

    [Header("Record/Playback")]
    [SerializeField] public Mode mode = Mode.Record;

    [SerializeField] public UpdateFunction UpdateCycle = UpdateFunction.Both;
    [SerializeField] private bool running = false;

    [Header("Movement Controller")]
    [SerializeField] private LayerMask lmWalls;

    [SerializeField] private float fJumpVelocity = 5;

    // Input sequences
    [SerializeField] public InputSequence oldSequence;

    [SerializeField] public InputSequence currentSequence;
    [SerializeField] public InputSequence nextSequence;
    public Animator animator;
    public SpriteRenderer sr;
    public SpriteRenderer shadowsr;
    private Rigidbody2D rigid;
    private float fJumpPressedRemember = 0;
    private float fGroundedRemember = 0;
    [SerializeField] private float fJumpPressedRememberTime = 0.2f;
    [SerializeField] private float fGroundedRememberTime = 0.25f;
    [SerializeField] private float fHorizontalAcceleration = 1;
    [SerializeField] [Range(0, 1)] private float fHorizontalDampingBasic = 0.5f;
    [SerializeField] [Range(0, 1)] private float fHorizontalDampingWhenStopping = 0.5f;
    [SerializeField] [Range(0, 1)] private float fHorizontalDampingWhenTurning = 0.5f;
    [SerializeField] [Range(0, 1)] private float fCutJumpHeight = 0.5f;

    public float start_time = 0;
    public int current_level;
    public bool is_history_player;
    private bool jump_off_platform = false;
    private int wall_mask;
    private int player_mask;

    [Header("LevelData")]
    [SerializeField] private GameData level_data;

    public LevelManager level_manager;

    [Header("Audio")]
    //[SerializeField] AudioClip audio_walk;
    [SerializeField] private AudioClip audio_jump;

    private AudioSource audio;

    private void StartPlayer(GameObject player, int level, bool playback) {
        level_manager.SetGameObjectToPos(player, level);
        is_history_player = playback;
        if (playback == true) {
            StartPlayBack();
        } else {
            StartRecord();
        }
    }

    private float setStartTime() {
        return Time.time;
    }

    private float getTime() {
        return Time.time - start_time;
    }

    private void Start() {
       
        audio = GetComponent<AudioSource>();
        rigid = GetComponent<Rigidbody2D>();
        wall_mask = LayerMask.NameToLayer("Wall");
        player_mask = LayerMask.NameToLayer("Player");
        Physics2D.IgnoreLayerCollision(player_mask, wall_mask, false);
        if (is_history_player == true) {
            StartPlayer(gameObject, current_level, true);
        } else {
            StartPlayer(gameObject, current_level, false);
        }
        animator = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        Transform t = gameObject.transform.Find("shadow");
        if (t != null) {
            shadowsr = t.gameObject.GetComponent<SpriteRenderer>();
        }
        if (shadowsr != null) {
            shadowsr.enabled = false;
        }
    }

    public void Stop() {
        running = false;
    }

    public bool StartRecord() {
        oldSequence.init();
        currentSequence.init();
        nextSequence.init();

        if (start_time == 0 && is_history_player == false) {
            start_time = setStartTime();
        }

        running = true;
        mode = Mode.Record;
        current_pos = 0;
        inputRecordStream = new StreamWriter(fs);  // will overwrite new file Stream
        if (inputRecordStream.ToString() == "") {
            Debug.Log("InputReplay: StreamWriter(), file not found ?");
            return false;
        } else {
            //Everthing is ok
            inputRecordStream.AutoFlush = true;
            return true;
        }
    }

    public bool StartPlayBack() {
        oldSequence.init();
        currentSequence.init();
        nextSequence.init();
        current_pos = 0;
        mode = Mode.PlayBack;
        eof = false;
        running = true;
        inputPlaybackStream = new StreamReader(fs, false);
        level_manager.SetGameObjectToPos(gameObject, current_level);
        if (inputPlaybackStream.ToString() == "") {
            Debug.Log("InputReplay: StreamReader(), file not found ?");
            return false;
        } else if (!ReadLine()) {
            Debug.Log("InputReplay: empty file");
            return false;
        } else {
            return false;
        }
    }

    public void Hide() {
        SpriteRenderer psr = gameObject.GetComponent<SpriteRenderer>();
        psr.enabled = false;
    }

    public void Show() {
        SpriteRenderer psr = gameObject.GetComponent<SpriteRenderer>();
        psr.enabled = true;
    }

    public void WritePositionData() {
        currentSequence.init();
        oldSequence.init();
        currentSequence.x = gameObject.transform.position.x;
        currentSequence.y = gameObject.transform.position.y;
        currentSequence.t = getTime();
        fs.Position = current_pos;
        inputRecordStream.WriteLine(JsonUtility.ToJson(currentSequence));
        current_pos = fs.Position;
    }

    public void GetPositionData() {
        if (mode == Mode.PlayBack && running == true) {
            if (getTime() >= nextSequence.t) {
                oldSequence = currentSequence;
                currentSequence = nextSequence;
                nextSequence.init();
                if (!ReadLine()) {
                    eof = true;
                    Stop();
                    level_manager.EndGoal(gameObject);
                    //Debug.Log("InputPlayback: EndOfFile");
                }
            }
        }
    }

    private bool ReadLine() // read a new line in file for the next sequence to play
    {
        fs.Position = current_pos;
        string newline = inputPlaybackStream.ReadLine();
        current_pos = fs.Position;
        if (newline == null) { return false; }
        try {
            nextSequence = JsonUtility.FromJson<InputSequence>(newline);
            return true;
        } catch {
            return false;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.tag == "WALL") {
            bGrounded = true;
            if (shadowsr != null) {
                shadowsr.enabled = true;
                jump_off_platform = false;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision) {
        if (collision.gameObject.tag == "WALL") {
            bGrounded = false;
            if (shadowsr != null) {
                shadowsr.enabled = false;
            }
        }
    }

    private IEnumerator JumpOffPlatform() {
        jump_off_platform = true;
        Physics2D.IgnoreLayerCollision(player_mask, wall_mask, true);
        yield return new WaitForSeconds(0.2f);
        Physics2D.IgnoreLayerCollision(player_mask, wall_mask, false);
        jump_off_platform = false;
    }

    private void Update() {
        bool jumped = false;
        if (running == true) {
            if (mode == Mode.PlayBack) {
                if (level_manager.time_frozen == false) {
                    GetPositionData();
                    if (eof != true) {
                        gameObject.transform.position = new Vector3(currentSequence.x, currentSequence.y, 0);
                        if (oldSequence.x > currentSequence.x) {
                            sr.flipX = true;
                        } else {
                            sr.flipX = false;
                        }
                        if (animator != null) {
                            float velocity = 0;
                            float speed = Mathf.Abs(currentSequence.x - oldSequence.x);
                            if (speed > 0) {
                                velocity = (currentSequence.x - oldSequence.x) / Time.deltaTime;
                            } else {
                                velocity = 0;
                            }
                            animator.speed = Mathf.Abs(velocity * 0.1f);
                            animator.SetFloat("speed", Mathf.Abs(speed));
                        }
                    }
                }
            } else {
                if (Input.GetButtonDown("Fire1")) {
                    //This will fix time incursion or freeze time
                    level_manager.TimeCrystalUsed();
                }

                fGroundedRemember -= Time.deltaTime; ;
                if (bGrounded) {
                    fGroundedRemember = fGroundedRememberTime;
                }

                fJumpPressedRemember -= Time.deltaTime;
                if (Input.GetButtonDown("Jump")) {
                    jumped = true;
                    fJumpPressedRemember = fJumpPressedRememberTime;
                }

                if (Input.GetButtonUp("Jump")) {
                    if (rigid.velocity.y > 0) {
                        rigid.velocity = new Vector2(rigid.velocity.x, rigid.velocity.y * fCutJumpHeight);
                    }
                }

                if ((Input.GetButtonDown("Down") || Input.GetAxisRaw("Vertical") < 0) && bGrounded && jump_off_platform == false) {
                    StartCoroutine(JumpOffPlatform());
                }

                if ((fJumpPressedRemember > 0) && (fGroundedRemember > 0)) {
                    fJumpPressedRemember = 0;
                    fGroundedRemember = 0;
                    rigid.velocity = new Vector2(rigid.velocity.x, fJumpVelocity);
                }

                float input = Input.GetAxisRaw("Horizontal");
                float fHorizontalVelocity = rigid.velocity.x;
                fHorizontalVelocity += input;

                if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.01f)
                    fHorizontalVelocity *= Mathf.Pow(1f - fHorizontalDampingWhenStopping, Time.deltaTime * 10f);
                else if (Mathf.Sign(Input.GetAxisRaw("Horizontal")) != Mathf.Sign(fHorizontalVelocity))
                    fHorizontalVelocity *= Mathf.Pow(1f - fHorizontalDampingWhenTurning, Time.deltaTime * 10f);
                else
                    fHorizontalVelocity *= Mathf.Pow(1f - fHorizontalDampingBasic, Time.deltaTime * 10f);

                rigid.velocity = new Vector2(fHorizontalVelocity, rigid.velocity.y);
                if (mode == Mode.Record) {
                    WritePositionData();
                }

                if (fHorizontalVelocity < 0) {
                    sr.flipX = true;
                } else {
                    sr.flipX = false;
                }
                float verlocity = Mathf.Abs(rigid.velocity.x * 0.1f);
                animator.speed = verlocity;
                animator.SetFloat("speed", Mathf.Abs(rigid.velocity.x));
                if (animator != null) {
                    // Debug.Log(rigid.velocity.x.ToString());
                }

                if (jumped == true && bGrounded == true) {
                    if (audio != null && audio_jump != null) {
                        audio.PlayOneShot(audio_jump);
                    }
                } else {
                    if (Mathf.Abs(input) > 0 && bGrounded == true) {
                        if (!audio.isPlaying) {
                            audio.Play();
                        }
                    } else if (Mathf.Abs(input) == 0 && bGrounded == true) {
                        audio.Stop();
                    }
                }
            }
        }
    }
}