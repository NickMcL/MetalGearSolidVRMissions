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

    // Player collider values
    Vector3 PLAYER_STANDING_COLLIDER_CENTER = new Vector3(0f, 0.5f, 0f);
    Vector3 PLAYER_STANDING_COLLIDER_SIZE = new Vector3(1f, 2f, 1f);
    Vector3 PLAYER_CRAWLING_COLLIDER_CENTER = new Vector3(0f, 0f, 0f);
    Vector3 PLAYER_CRAWLING_COLLIDER_SIZE = new Vector3(1f, 1f, 1f);
    Vector3 PLAYER_UNDER_OBSTACLE_COLLIDER_CENTER = new Vector3(0f, 0.7f, 0f);
    Vector3 PLAYER_UNDER_OBSTACLE_COLLIDER_SIZE = new Vector3(1f, 0.4f, 1f);

    public static MovementController player;  // Singleton
    Rigidbody body;

    public float run_speed = 10f;
    public float crawl_speed = 2f;
    public float rot_speed = 10f;
    public float control_change_delay = 1.0f;

    public enum movementState {
        RUN,
        CRAWL,
        AGAINST_WALL,
        ALONG_WALL
    };
    public movementState move_state = movementState.RUN;

    public Vector3 locked_direction;
    bool hit_corner = false;
    bool under_obstacle = false;

    // Use this for initialization
    void Start() {
        player = this;
        body = gameObject.GetComponent<Rigidbody>();
    }

	// Update is called once per frame
	void Update () {
        if (under_obstacle) {
            updateUnderObstacleTransformFromInput();
        } else {
            setVelocityFromInput();
            updateWallMovement();
            updateForwardDirection();
        }

        if (Input.GetKey(ATTACK_KEY)) {
            if (!knock_lock) {
                knock_lock = true;
                Invoke("unlock_knock", 1f);
                Knock();
            }
        }

        if (Input.GetKeyDown(CRAWL_KEY)) {
            toggleCrawl();
        }

        adjustCamera();
        adjustPlayerCollider();
	}

    void unlock_knock() {
        knock_lock = false;
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

        if (move_state == movementState.CRAWL) {
            body.velocity = vel.normalized * crawl_speed;
        } else {
            body.velocity = vel.normalized * run_speed;
        }
    }

    void updateUnderObstacleTransformFromInput() {
        Vector3 vel = Vector3.zero;
        if (Input.GetKey(LEFT_KEY)) {
            this.transform.Rotate(Vector3.forward * rot_speed * Time.deltaTime);  // Rotate left
        }
        if (Input.GetKey(RIGHT_KEY)) {
            this.transform.Rotate(-Vector3.forward * rot_speed * Time.deltaTime);  // Rotate right
        }
        if (Input.GetKey(UP_KEY)) {
            vel += this.transform.up;  // Move forward
        }
        if (Input.GetKey(DOWN_KEY)) {
            vel -= this.transform.up;  // Move back
        }
        body.velocity = vel.normalized * crawl_speed;
        this.transform.eulerAngles = new Vector3(90f, this.transform.eulerAngles.y,
            this.transform.eulerAngles.z);
    }

    void updateWallMovement() {
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

    void Knock() {
        GameObject[] all_Enemy = GameObject.FindGameObjectsWithTag("Enemy");
        float range = 20f;
        GameObject[] all_point = GameObject.FindGameObjectsWithTag("Waypoint");
        foreach (GameObject point in all_point) {
            if (point.transform.position==transform.position)
                return;
        }

        GameObject current_player_point = Instantiate(waypoint_prefab, transform.position, Quaternion.identity) as GameObject;
        current_player_point.name = "player_pos" + poscount++;
        
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
        if (move_state != movementState.CRAWL) {
            body.transform.position = new Vector3(body.transform.position.x, .25f, body.transform.position.z);
            if (move_state == movementState.AGAINST_WALL) {
                // Move off of the wall before lying down to crawl
                body.transform.position += body.transform.forward * (this.transform.localScale.y / 2f);
            }
            body.transform.Rotate(new Vector3(90f, 0f, 0f));
            move_state = movementState.CRAWL;
        } else if (move_state == movementState.CRAWL &&
                !Physics.Raycast(this.transform.position, this.transform.forward * -1f)) {
            body.transform.position = new Vector3(body.transform.position.x, 1f, body.transform.position.z);
            body.transform.Rotate(new Vector3(-90f, 0f, 0f));
            move_state = movementState.RUN;
        }
    }

    void adjustCamera() {
        bool camera_moved = false;

        if (move_state == movementState.AGAINST_WALL) {
            camera_moved = moveCameraIfByWallEdge();
        } else if (move_state == movementState.CRAWL) {
            camera_moved = moveCameraIfUnderObstacle();
            under_obstacle = camera_moved;
        }

        if (!camera_moved) {
            CameraController.cam_control.moveToOverviewPosition();
        }
    }

    bool moveCameraIfByWallEdge() {
        Vector3 right_check_pos = this.transform.position;
        Vector3 left_check_pos = this.transform.position;
        int layer_mask = 1 << LayerMask.NameToLayer("Obstacle");
        right_check_pos.y += 1.25f;
        left_check_pos.y += 1.25f;

        right_check_pos += this.transform.right * (1.1f + (this.transform.localScale.x / 2f));
        right_check_pos += this.transform.forward * -0.5f;
        left_check_pos += this.transform.right * -(1.1f + (this.transform.localScale.x / 2f));
        left_check_pos += this.transform.forward * -0.5f;

        Debug.DrawRay(this.transform.position, right_check_pos - this.transform.position);
        Debug.DrawRay(this.transform.position, left_check_pos - this.transform.position);
        if (!Physics.CheckSphere(right_check_pos, 0.01f, layer_mask)) {
            CameraController.cam_control.moveToLookDownHallway(this.transform, true);
            return true;
        } else if (!Physics.CheckSphere(left_check_pos, 0.01f, layer_mask)) {
            CameraController.cam_control.moveToLookDownHallway(this.transform, false);
            return true;
        }
        return false;
    }

    bool moveCameraIfUnderObstacle() {
        bool hit;
        RaycastHit hit_info;

        hit = Physics.Raycast(this.transform.position, this.transform.forward * -1f, out hit_info);
        if (hit && hit_info.collider.gameObject.tag == "Obstacle") {
            CameraController.cam_control.moveToUnderObstacle(this.transform);
            return true;
        }
        return false;
    }

    void adjustPlayerCollider() {
        BoxCollider player_collider = gameObject.GetComponent<BoxCollider>();
        if (move_state != movementState.CRAWL) {
            player_collider.center = PLAYER_STANDING_COLLIDER_CENTER;
            player_collider.size = PLAYER_STANDING_COLLIDER_SIZE;
            return;
        }

        if (under_obstacle) {
            player_collider.center = PLAYER_UNDER_OBSTACLE_COLLIDER_CENTER;
            player_collider.size = PLAYER_UNDER_OBSTACLE_COLLIDER_SIZE;
        } else {
            player_collider.center = PLAYER_CRAWLING_COLLIDER_CENTER;
            player_collider.size = PLAYER_CRAWLING_COLLIDER_SIZE;
        }
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
