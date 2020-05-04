using System;
using System.IO;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    public enum Mode { Record, PlayBack };

    private bool eof = false;
    private long current_pos = 0;

    public enum UpdateFunction { FixedUpdate, Update, Both };

    [Header("Record/Playback")]
    [SerializeField] public Mode mode = Mode.Record;

    [SerializeField] public UpdateFunction UpdateCycle = UpdateFunction.Both;
    [SerializeField] public string record_path = "Temp/input.json";
    private float startTime = 0.0f;
    [SerializeField] private bool active = false;

    // private types
    [Serializable]
    public struct InputSequence        // var names are reduced for smaller json
    {
        public float t;         // time
        public bool vBD;    // virtual Button Down
        public bool vBU;    // virtual Button Up
        public float vA;      // virtual Axis
        public float x;
        public float y;

        public void init() {
            vBD = false;
            vBU = false;
            vA = 0;
            x = 0;
            y = 0;
        }
    };

    [Header("Movement Controller")]
    [SerializeField] private LayerMask lmWalls;

    [SerializeField] private float fJumpVelocity = 5;

    private StreamReader inputPlaybackStream;
    private StreamWriter inputRecordStream;
    public FileStream fs;
    [SerializeField] private SpawnPointList spawnpoints;

    // Input sequences
    private InputSequence oldSequence;

    private InputSequence currentSequence;
    private InputSequence nextSequence;

    private void setStartTime() {
        if (UpdateCycle == UpdateFunction.Update)
            startTime = Time.fixedTime;
        else
            startTime = Time.time;
    }

    private InputReplay myInput;
    private Rigidbody2D rigid;

    private float fJumpPressedRemember = 0;

    [SerializeField] private float fJumpPressedRememberTime = 0.2f;

    private float fGroundedRemember = 0;

    [SerializeField] private float fGroundedRememberTime = 0.25f;

    [SerializeField] private float fHorizontalAcceleration = 1;

    [SerializeField] [Range(0, 1)] private float fHorizontalDampingBasic = 0.5f;

    [SerializeField] [Range(0, 1)] private float fHorizontalDampingWhenStopping = 0.5f;

    [SerializeField] [Range(0, 1)] private float fHorizontalDampingWhenTurning = 0.5f;

    [SerializeField] [Range(0, 1)] private float fCutJumpHeight = 0.5f;

    private void Start() {
        rigid = GetComponent<Rigidbody2D>();
        myInput = gameObject.GetComponent<InputReplay>();
    }

    public void Stop() {
        active = false;
        switch (mode) { // close streams
            case Mode.Record:
                // if (inputRecordStream != null) { inputRecordStream.Flush(); }
                break;

            case Mode.PlayBack:
                // if (inputPlaybackStream != null) { inputPlaybackStream.Close(); }
                break;
        }
    }

    public bool StartRecord() {
        oldSequence.init();
        currentSequence.init();
        nextSequence.init();
        current_pos = fs.Position;
        setStartTime();
        active = true;
        mode = Mode.Record;

        inputRecordStream = new StreamWriter(fs);  // will overwrite new file Stream
        if (inputRecordStream.ToString() == "") {
            Stop();
            Debug.Log("InputReplay: StreamWriter(" + record_path + "), file not found ?");
            return false;
        } else {
            rigid = GetComponent<Rigidbody2D>();
            rigid.velocity = new Vector2(0, 0);
            inputRecordStream.AutoFlush = true;
            return true;
        }
    }

    public bool StartPlayBack() {
        eof = false;
        oldSequence.init();
        currentSequence.init();
        nextSequence.init();

        setStartTime();
        active = true;
        mode = Mode.PlayBack;
        this.current_pos = 0;
        fs.Position = current_pos;

        inputPlaybackStream = new StreamReader(fs, false);
        PlayerLevelManager plm = gameObject.GetComponent<PlayerLevelManager>();
        gameObject.transform.position = spawnpoints.spawnpoints[plm.this_level].transform.position;
        if (inputPlaybackStream.ToString() == "") {
            Stop();
            Debug.Log("InputReplay: StreamReader(" + record_path + "), file not found ?");
            return false;
        } else if (!ReadLine()) {
            Stop();
            Debug.Log("InputReplay: empty file");
            return false;
        } else {
            rigid = GetComponent<Rigidbody2D>();
            rigid.velocity = new Vector2(0, 0);
            setStartTime();
            return false;
        }
    }

    public void GetPos() {
        float time = 0;
        if (UpdateCycle == UpdateFunction.Update || UpdateCycle == UpdateFunction.Both) {
            time = Time.time - startTime;
        } else {
            time = Time.fixedTime - startTime;
        }
        currentSequence.init();
        currentSequence.x = gameObject.transform.position.x;
        currentSequence.y = gameObject.transform.position.y;
        fs.Position = current_pos;
        currentSequence.t = time;
        inputRecordStream.WriteLine(JsonUtility.ToJson(currentSequence));
        current_pos = fs.Position;
    }

    public InputSequence GetInput() {
        float time = 0;
        if (UpdateCycle == UpdateFunction.Update || UpdateCycle == UpdateFunction.Both) {
            time = Time.time - startTime;
        } else {
            time = Time.fixedTime - startTime;
        }

       /* if (mode == Mode.Record && active == true) {
            currentSequence.init();
            if (Input.GetButtonUp("Jump")) { currentSequence.vBU = true; }
            if (Input.GetButtonDown("Jump")) { currentSequence.vBD = true; }
            currentSequence.vA = Input.GetAxisRaw("Horizontal");
            currentSequence.x = gameObject.transform.position.x;
            currentSequence.y = gameObject.transform.position.y;
            currentSequence.t = time;
            // only write if something changed
            //if (AnyChange(oldSequence, currentSequence)) {
            //Debug.Log (JsonUtility.ToJson (newSequence));;
            fs.Position = current_pos;
            inputRecordStream.WriteLine(JsonUtility.ToJson(currentSequence));
            current_pos = fs.Position;
            oldSequence = currentSequence;*/
            // }
        if (mode == Mode.PlayBack && active == true) {
            if (time >= nextSequence.t) {
                oldSequence = currentSequence;
                currentSequence = nextSequence;
                //Debug.Log (time);

                nextSequence.init();
                if (!ReadLine()) {
                    Stop();
                    eof = true;
                    Debug.Log("InputPlayback: EndOfFile");
                }
            }
        }
        return currentSequence;
    }

    private bool AnyChange(InputSequence seqA, InputSequence seqB) {
        if (seqA.vA == seqB.vA && seqA.vBD == seqB.vBD && seqA.vBU == seqB.vBU) return false;
        else return true;
    }

    private void Play(float time) {
        if (time >= nextSequence.t) {
            oldSequence = currentSequence;
            currentSequence = nextSequence;
            //Debug.Log (time);

            nextSequence.init();
            if (!ReadLine()) {
                Stop();
                Debug.Log("InputPlayback: EndOfFile");
                eof = true;
            }
        }
    }

    private bool ReadLine() // read a new line in file for the next sequence to play
    {
        fs.Position = current_pos;
        string newline = inputPlaybackStream.ReadLine();

        if (newline == null)
            return false;

        nextSequence = JsonUtility.FromJson<InputSequence>(newline);
        current_pos = fs.Position;
        return true;
    }
    private void FixedUpdate() {
        if (mode == Mode.Record) {
            GetPos();
           
        }
    }

    private void Update() {
        if (mode == Mode.PlayBack) {
            InputSequence user_input = GetInput();
            if (eof == true) {
                GetComponent<PlayerLevelManager>().EndGoal();
            } else {
                gameObject.transform.position = new Vector3(currentSequence.x, currentSequence.y, 0);
            }
        } else {
            Vector2 v2GroundedBoxCheckPosition = (Vector2)transform.position + new Vector2(0, -0.01f);
            Vector2 v2GroundedBoxCheckScale = (Vector2)transform.localScale + new Vector2(-0.02f, 0);
            bool bGrounded = Physics2D.OverlapBox(v2GroundedBoxCheckPosition, v2GroundedBoxCheckScale, 0, lmWalls);

            fGroundedRemember -= Time.deltaTime;
            if (bGrounded) {
                fGroundedRemember = fGroundedRememberTime;
            }

            //Lets get user input
            InputSequence user_input = GetInput();

            fJumpPressedRemember -= Time.deltaTime;
            if (Input.GetButtonDown("Jump")) {
                fJumpPressedRemember = fJumpPressedRememberTime;
            }

            if (Input.GetButtonUp("Jump")) {
                if (rigid.velocity.y > 0) {
                    rigid.velocity = new Vector2(rigid.velocity.x, rigid.velocity.y * fCutJumpHeight);
                }
            }

            if ((fJumpPressedRemember > 0) && (fGroundedRemember > 0)) {
                fJumpPressedRemember = 0;
                fGroundedRemember = 0;
                rigid.velocity = new Vector2(rigid.velocity.x, fJumpVelocity);
            }

            float fHorizontalVelocity = rigid.velocity.x;
            fHorizontalVelocity += Input.GetAxisRaw("Horizontal");

            if (Mathf.Abs(Input.GetAxisRaw("Horizontal")) < 0.01f)
                fHorizontalVelocity *= Mathf.Pow(1f - fHorizontalDampingWhenStopping, Time.deltaTime * 10f);
            else if (Mathf.Sign (Input.GetAxisRaw("Horizontal")) != Mathf.Sign(fHorizontalVelocity))
                fHorizontalVelocity *= Mathf.Pow(1f - fHorizontalDampingWhenTurning, Time.deltaTime * 10f);
            else
                fHorizontalVelocity *= Mathf.Pow(1f - fHorizontalDampingBasic, Time.deltaTime * 10f);

            rigid.velocity = new Vector2(fHorizontalVelocity, rigid.velocity.y);
        }
    }
}