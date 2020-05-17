using UnityEngine;

public class Game2DTrigger : MonoBehaviour {
    [SerializeField] public LevelManager level_manager;
    AudioSource audioSource;
    [SerializeField] public AudioClip impact;
    [SerializeField] public bool death=false;

    void Start() {
        audioSource = level_manager.GetComponent<AudioSource>();
    }


    private void OnTriggerEnter2D(Collider2D collision) {


        if (death == true && collision.tag.ToUpper() == "PLAYER") {
            if (level_manager != null) {
                if (impact != null) {
                    audioSource.PlayOneShot(impact, 1F);
                }
                level_manager.Player_Died();
            }
        }

        if (collision.tag.ToUpper() == "PLAYER" && gameObject.tag.ToUpper() == "HISTORY_PLAYER") {
            if (level_manager != null) {

                if (level_manager.invincible == false) { 
                    if (audioSource != null) { 
                        audioSource.PlayOneShot(impact, 1F); 
                    }
                }
                level_manager.Collition_With_History_Player();

                
            }
        } else if (collision.tag.ToUpper() == "PLAYER" && gameObject.tag.ToUpper() == "GOAL") {
            if (level_manager != null) {
                if (audioSource != null) { audioSource.PlayOneShot(impact, 0.7F); }
                level_manager.EndGoal(collision.gameObject);
            }
        } else if (collision.tag.ToUpper() == "PLAYER" && gameObject.tag.ToUpper() == "SPAWN_ITEM") {
            if (level_manager != null) { 
                level_manager.ItemCollected(gameObject);
                if (audioSource != null) { audioSource.PlayOneShot(impact, 0.7F); }
            }
        } else if (collision.tag.ToUpper() == "PLAYER" && gameObject.tag.ToUpper() == "INCUR_FIX") {
            if (level_manager != null) {
                level_manager.Time_Crystal_Collected(gameObject);
                if (audioSource != null) { audioSource.PlayOneShot(impact, 0.7F); }
            }
        } else if (collision.tag.ToUpper() == "PLAYER" && gameObject.tag.ToUpper() == "TRANSPORT") {
            if (level_manager != null) {
                Target target= gameObject.GetComponent<Target>();
                if (target != null) { 
                    level_manager.TransportTo(collision.gameObject, target);
                }
            }

        }
    }
}