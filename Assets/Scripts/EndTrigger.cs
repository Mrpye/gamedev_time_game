using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision) {
        GameObject Player = collision.gameObject;
        PlayerLevelManager plm=Player.GetComponent<PlayerLevelManager>();

        plm.EndGoal();


    }
}
