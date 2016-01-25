using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovementController : MonoBehaviour {
    // Key codes
    public GameObject waypoint_prefab;
    const KeyCode UP_KEY = KeyCode.UpArrow;
    const KeyCode LEFT_KEY = KeyCode.LeftArrow;
    const KeyCode DOWN_KEY = KeyCode.DownArrow;
    const KeyCode RIGHT_KEY = KeyCode.RightArrow;
    const KeyCode CRAWL_KEY = KeyCode.Q;
    const KeyCode ATTACK_KEY = KeyCode.A;
    float poscount = 0;
    bool knock_lock;

    public static MovementController player;  // Singleton

    public float speed = 10f;
    Rigidbody body;

    public enum movementState {
        RUN,
        CRAWL,
        AGAINST_WALL
    };
    public movementState curr_state = movementState.RUN;

    // Use this for initialization
    void Start() {
        player = this;
        body = gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update() {
        moveFromInput();

        if (Input.GetKey(ATTACK_KEY)) {
            if (!knock_lock) {
                knock_lock=true;
                Invoke("unlock_knock", 1f);
                Knock();
                
            }
        }

        if (Input.GetKeyDown(CRAWL_KEY)) {
            toggleCrawl();
        }
    }
    void unlock_knock() {
        knock_lock=false;
    }
    void moveFromInput() {
        Vector3 vel = Vector3.zero;
        if (Input.GetKey(UP_KEY)) {
            vel.z += 1;  // Move up
        }
        if (Input.GetKey(DOWN_KEY)) {
            vel.z -= 1;  // Move down
        }
        if (Input.GetKey(LEFT_KEY)) {
            vel.x -= 1;  // Move left
        }
        if (Input.GetKey(RIGHT_KEY)) {
            vel.x += 1;  // Move right
        }
        body.velocity = vel.normalized * speed;

        if (vel != Vector3.zero) {
            gameObject.transform.forward = vel.normalized;
            if (curr_state == movementState.CRAWL) {
                body.transform.Rotate(new Vector3(90f, 0f, 0f));
            }
        }
    }

    void Knock() {
        GameObject[] all_Enemy= GameObject.FindGameObjectsWithTag("Enemy");
        float range = 20f;
        GameObject[] all_point= GameObject.FindGameObjectsWithTag("Waypoint");
        foreach (GameObject point in all_point) {
            if (point.transform.position==transform.position)
                return;
        }

        GameObject current_player_point = Instantiate(waypoint_prefab, transform.position, Quaternion.identity) as GameObject;
        
        current_player_point.name="player_pos"+poscount++;
        
        foreach (GameObject grunt in all_Enemy) {

            Vector3 to_player = grunt.transform.position-transform.position;
            if (to_player.magnitude < range) {
                grunt.GetComponent<Enemy>().investigate(current_player_point);
                current_player_point.GetComponent<PatrolPoint>().waiting++;
            }

        }
        current_player_point.GetComponent<PatrolPoint>().waiting--;
    }
    void toggleCrawl() {
        Vector3 above_snake = this.transform.position; //used to stop player from uncrouching when underneath something
        above_snake.y += this.transform.lossyScale.z / 2F + .01F;
        Debug.DrawRay(above_snake, this.transform.forward * -2);

        if (curr_state == movementState.RUN) {
            body.transform.Rotate(new Vector3(90f, 0f, 0f));
            body.transform.position = new Vector3(body.transform.position.x, .25f, body.transform.position.z);
            curr_state = movementState.CRAWL;
        }
        else if (curr_state == movementState.CRAWL && !Physics.Raycast(above_snake, this.transform.forward * -2)) {
            body.transform.Rotate(new Vector3(-90f, 0f, 0f));
            body.transform.position = new Vector3(body.transform.position.x, 1f, body.transform.position.z);
            curr_state = movementState.RUN;
        }
    }

    void OnTriggerEnter(Collider coll) {
        if (coll.gameObject.tag == "Obstacle" && curr_state == movementState.RUN) {
            curr_state = movementState.AGAINST_WALL;
            //body.transform.Rotate(new Vector3());
        }
    }
}
