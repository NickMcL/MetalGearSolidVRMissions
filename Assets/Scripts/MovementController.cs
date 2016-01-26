using UnityEngine;
using System.Collections;

public class MovementController : MonoBehaviour {
    // Key codes
    const KeyCode UP_KEY = KeyCode.UpArrow;
    const KeyCode LEFT_KEY = KeyCode.LeftArrow;
    const KeyCode DOWN_KEY = KeyCode.DownArrow;
    const KeyCode RIGHT_KEY = KeyCode.RightArrow;
    const KeyCode CRAWL_KEY = KeyCode.Q;

    // Player collider values
    Vector3 PLAYER_STANDING_COLLIDER_CENTER = new Vector3(0f, 0.5f, 0f);
    Vector3 PLAYER_STANDING_COLLIDER_SIZE = new Vector3(1f, 2f, 1f);
    Vector3 PLAYER_CRAWLING_COLLIDER_CENTER = new Vector3(0f, 0f, 0f);
    Vector3 PLAYER_CRAWLING_COLLIDER_SIZE = new Vector3(1f, 1f, 1f);

    public static MovementController player;  // Singleton

    public float speed = 10f;
    Rigidbody body;

    public enum movementState {
        RUN,
        CRAWL,
        AGAINST_WALL,
        ALONG_WALL
    };
    public movementState move_state = movementState.RUN;

    public Vector3 locked_direction;
    bool hit_corner = false;

	// Use this for initialization
	void Start () {
        player = this;
        body = gameObject.GetComponent<Rigidbody>();
	}

	// Update is called once per frame
	void Update () {
        setVelocityFromInput();
        if (nextToObstacle() || hit_corner) {
            hit_corner = false;
            if (move_state == movementState.ALONG_WALL) {
                updateAlongWall();
            }
            else if (move_state == movementState.AGAINST_WALL) {
                updateAgainstWall();
            }
        } else if (move_state != movementState.CRAWL) {
            move_state = movementState.RUN;
            locked_direction = Vector3.zero;
        }
        updateForwardDirection();

        if (Input.GetKeyDown(CRAWL_KEY)) {
            toggleCrawl();
        }

        if (move_state == movementState.AGAINST_WALL) {
            moveCameraIfByWallEdge();
        }
	}

    void setVelocityFromInput() {
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
    }

    bool nextToObstacle() {
        bool hit_front, hit_back, hit_right, hit_left;
        RaycastHit hit_front_info, hit_back_info, hit_right_info, hit_left_info;
        Vector3 start_vector = this.transform.position;
        start_vector.y += 1.25f;

        hit_front = Physics.Raycast(start_vector, this.transform.forward, out hit_front_info, 1f);
        hit_back = Physics.Raycast(start_vector, this.transform.forward * -1f, out hit_back_info, 1f);
        hit_right = Physics.Raycast(start_vector, this.transform.right, out hit_right_info, 1f);
        hit_left = Physics.Raycast(start_vector, this.transform.right * -1f, out hit_left_info, 1f);
        Debug.DrawRay(start_vector, this.transform.forward * 1f);
        Debug.DrawRay(start_vector, this.transform.forward * -1f);
        Debug.DrawRay(start_vector, this.transform.right * 1f);
        Debug.DrawRay(start_vector, this.transform.right * -1f);
        if ((hit_front && hit_front_info.collider.gameObject.tag == "Obstacle") ||
                (hit_back && hit_back_info.collider.gameObject.tag == "Obstacle") ||
                (hit_right && hit_right_info.collider.gameObject.tag == "Obstacle") ||
                (hit_left && hit_left_info.collider.gameObject.tag == "Obstacle")) {
            return true;
        }
        return false;
    }

    void updateAlongWall() {
        if (movingInLockedDirection()) {
            if (!movingDiagonal()) {
                move_state = movementState.AGAINST_WALL;
                body.transform.rotation = Quaternion.LookRotation(locked_direction * -1);
                moveBackAgainstWall();
            }
            zeroMovementInLockedDirection();

        } else if (movingOppositeOfLockedDirection()) {
            locked_direction = Vector3.zero;
            move_state = movementState.RUN;
        }
    }

    void moveBackAgainstWall() {
        bool hit;
        RaycastHit hit_info;
        Vector3 start_vector = this.transform.position;
        start_vector.y += 1.25f;

        hit = Physics.Raycast(start_vector, body.transform.forward * -1f, out hit_info);
        if (hit && hit_info.collider.gameObject.tag == "Obstacle") {
            this.transform.position += ((body.transform.forward * -1f) *
                    (hit_info.distance - this.transform.localScale.z / 2f));
        }
    }

    void updateAgainstWall() {
        if (movingOppositeOfLockedDirection()) {
            locked_direction = Vector3.zero;
            move_state = movementState.RUN;
            return;
        } 

        if (!movingInLockedDirection()) {
            this.transform.position += this.transform.forward * 0.5f;
            locked_direction = Vector3.zero;
            move_state = movementState.RUN;
        } else {
            zeroMovementInLockedDirection();
        }
    }

    void updateForwardDirection() {
        if (move_state == movementState.AGAINST_WALL) {
            return;
        }

        if (body.velocity != Vector3.zero) {
            gameObject.transform.forward = body.velocity.normalized;
            if (move_state == movementState.CRAWL) {
                body.transform.Rotate(new Vector3(90f, 0f, 0f));
            }
        }
    }

    void toggleCrawl() {
        BoxCollider player_collider = gameObject.GetComponent<BoxCollider>();
        Vector3 above_snake = this.transform.position; //used to stop player from uncrouching when underneath something
        above_snake.y += (this.transform.localScale.z / 2f) + .01f;

        if (move_state != movementState.CRAWL) {
            player_collider.center = PLAYER_CRAWLING_COLLIDER_CENTER;
            player_collider.size = PLAYER_CRAWLING_COLLIDER_SIZE;
            body.transform.position = new Vector3(body.transform.position.x, .25f, body.transform.position.z);
            if (move_state == movementState.AGAINST_WALL) {
                // Move off of the wall before lying down to crawl
                body.transform.position += body.transform.forward * (this.transform.localScale.y / 2f);
            }
            body.transform.Rotate(new Vector3(90f, 0f, 0f));
            move_state = movementState.CRAWL;
        } else if (move_state == movementState.CRAWL && !Physics.Raycast(above_snake, this.transform.forward * -1f)) {
            player_collider.center = PLAYER_STANDING_COLLIDER_CENTER;
            player_collider.size = PLAYER_STANDING_COLLIDER_SIZE;
            body.transform.position = new Vector3(body.transform.position.x, 1f, body.transform.position.z);
            body.transform.Rotate(new Vector3(-90f, 0f, 0f));
            move_state = movementState.RUN;
        }
    }

    void moveCameraIfByWallEdge() {

    }

    void OnCollisionEnter(Collision coll) {
        Vector3 pos;

        if (coll.gameObject.tag == "Obstacle" && move_state == movementState.RUN) {
            foreach (ContactPoint contact in coll.contacts) {
                if (contact.normal.x != 0 || contact.normal.z != 0) {
                    locked_direction = coll.contacts[0].normal * -1;
                    locked_direction.y = 0;
                    break;
                }
            }
            if (locked_direction.x != 0 && locked_direction.z != 0) {
                hit_corner = true;
                locked_direction.z = 0;
                pos = this.transform.position;
                pos.x += locked_direction.x * -0.5f;
                pos.z += body.velocity.normalized.z * -0.5f;
                this.transform.position = pos;
            }

            if (movingDiagonal()) {
                move_state = movementState.ALONG_WALL;
            } else {
                move_state = movementState.AGAINST_WALL;
                body.transform.rotation = Quaternion.LookRotation(coll.contacts[0].normal);
            }
        }
    }

    bool movingDiagonal() {
        return body.velocity.x != 0 && body.velocity.z != 0;
    }

    bool movingInLockedDirection() {
        return Vector3.Dot(body.velocity, locked_direction) > 0;
    }

    bool movingOppositeOfLockedDirection() {
        return Vector3.Dot(body.velocity, locked_direction) < 0;
    }

    void zeroMovementInLockedDirection() {
        Vector3 vel = body.velocity;
        if (locked_direction.x != 0) {
            vel.x = 0;
        } else if (locked_direction.z != 0) {
            vel.z = 0;
        }
        body.velocity = vel;
    }
}
