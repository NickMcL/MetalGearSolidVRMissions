using UnityEngine;
using System.Collections;

public class MovementController : MonoBehaviour {
    public float speed = 10f;
    Rigidbody body;

    public enum movementState { run, crawl }
    public movementState curr_state = movementState.run;
    public static MovementController player;

	// Use this for initialization
	void Start () {
        player = this;
        body = gameObject.GetComponent<Rigidbody>();
	}

	// Update is called once per frame
	void Update () {
        Vector3 vel = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) {
            vel.z += 1;  // Move up
        }
        if (Input.GetKey(KeyCode.S)) {
            vel.z -= 1;  // Move down
        }
        if (Input.GetKey(KeyCode.A)) {
            vel.x -= 1;  // Move left
        }
        if (Input.GetKey(KeyCode.D)) {
            vel.x += 1;  // Move right
        }
        body.velocity = vel.normalized * speed;

        if (vel != Vector3.zero) {
            gameObject.transform.forward = vel.normalized;
            if (curr_state == movementState.crawl) {
                body.transform.Rotate(new Vector3(90f, 0f, 0f));
            }
        }

        if (Input.GetKeyDown(KeyCode.Q)) {
            if (curr_state == movementState.run) {
                body.transform.Rotate(new Vector3(90f, 0f, 0f));
                body.transform.position = new Vector3(body.transform.position.x, .25f, body.transform.position.z);
                curr_state = movementState.crawl;
            } else if (curr_state == movementState.crawl) {
                body.transform.Rotate(new Vector3(-90f, 0f, 0f));
                body.transform.position = new Vector3(body.transform.position.x, 1f, body.transform.position.z);
                curr_state = movementState.run;
            }
        }
	}
}
