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
    const KeyCode GRAB_KEY = KeyCode.S;
    const KeyCode KNOCK_KEY = KeyCode.X;
    float poscount = 0;
    bool knock_lock = false;
    bool flip_lock = false;
    public LayerMask enemy_layer;
    GameObject victim;
    // Player collider values
    Vector3 PLAYER_STANDING_COLLIDER_CENTER = new Vector3(0f, 0.5f, 0f);
    Vector3 PLAYER_STANDING_COLLIDER_SIZE = new Vector3(1f, 2f, 1f);
    Vector3 PLAYER_CRAWLING_COLLIDER_CENTER = new Vector3(0f, 0f, 0f);
    Vector3 PLAYER_CRAWLING_COLLIDER_SIZE = new Vector3(1f, 1f, 1f);
    Vector3 PLAYER_UNDER_OBSTACLE_COLLIDER_CENTER = new Vector3(0f, 0.7f, 0f);
    Vector3 PLAYER_UNDER_OBSTACLE_COLLIDER_SIZE = new Vector3(1f, 0.4f, 1f);

    public static MovementController player;  // Singleton
    Rigidbody body;
    float choke_count = 0;
    float choke_timer = 0;
    public float run_speed = 10f;
    public float crawl_speed = 2f;
    public float grabbing_speed = 7f;
    public float against_wall_speed = 5f;
    public float rot_speed = 30f;
    public float control_change_delay = 0.5f;

    // Position limits
    float x_position_min;
    float x_position_max;
    float z_position_min;
    float z_position_max;

    public enum movementState {
        RUN,
        GRABBING,
        CRAWL,
        AGAINST_WALL,
        ALONG_WALL,
    };
    public movementState move_state = movementState.RUN;

    public Vector3 locked_direction;
    public bool under_obstacle_last_frame;
    Vector3 velocity_last_frame;
    bool hit_corner;
    float control_lock_start_time;

    public float spawn_duration = 1.0f;
    public float unspawn_duration = 0.5f;
    public Vector3 spawn_sphere_max_size = new Vector3(1.5f, 1.5f, 1.5f);
    public Material spawn_sphere_material;
    public GameObject spawn_sphere;
    bool spawning_player;
    bool unspawning_player;
    float spawn_start_time;

    void Awake() {
        player = this;
    }

    // Use this for initialization
    void Start() {
        body = gameObject.GetComponent<Rigidbody>();
        initPositionLimits();

        BoxCollider player_collider = gameObject.GetComponent<BoxCollider>();
        player_collider.center = PLAYER_STANDING_COLLIDER_CENTER;
        player_collider.size = PLAYER_STANDING_COLLIDER_SIZE;

        control_lock_start_time = 0f;
        under_obstacle_last_frame = false;
        hit_corner = false;
        spawning_player = false;
        unspawning_player = false;
    }

    // Update is called once per frame
    void Update() {
        if (spawning_player) {
            growSpawnSphere();
        }
        if (unspawning_player) {
            shrinkSpawnSphere();
        }
        if (!CameraController.cam_control.playerHasControl() || CameraController.cam_control.game_paused) {
            body.velocity = Vector3.zero;
            return;
        }

        if (move_state == movementState.GRABBING) {
            resolveGrab();
        }
        if (knock_lock || flip_lock) {
            return;
        }

        if (!lockControlsIfNeeded()) {
            if (under_obstacle_last_frame) {
                updateUnderObstacleTransformFromInput();
            }
            else {
                setVelocityFromInput();
                updateWallMovement();
                updateForwardDirection();
            }
        }

        keepPlayerWithinPositionLimits();

        if (Input.GetKey(KNOCK_KEY)) {
            if (!knock_lock) {
                body.velocity = Vector3.zero;
                if (movementState.AGAINST_WALL == move_state) {
                    knock_lock = true;
                    Knock();
                }
                else {
                    punchCheck();
                }
                Invoke("unlockKnock", 0.3f); //prevents knock spam
            }
        }

        if (Input.GetKey(GRAB_KEY)) {
            if (choke_timer <= 0) {
                resolveGrab();
                if (move_state == movementState.GRABBING) {
                    choke_count++;
                    AudioController.audioPlayer.chokeSound();
                    body.velocity = Vector3.zero;
                    knock_lock = true;
                    victim.GetComponent<Rigidbody>().AddForceAtPosition(victim.transform.forward * -87, victim.transform.position + victim.transform.up);
                    Invoke("unlockKnock", 0.1f);
                }
                choke_timer = 0.1f;
            }
            else {
                choke_timer -= Time.deltaTime;
            }
            if (choke_count > 10) {
                victim.GetComponent<Enemy>().die();
                choke_count = 0;
                move_state = movementState.RUN;
            }

        }

        if (Input.GetKeyDown(CRAWL_KEY)) {
            toggleCrawl();
        }

        adjustCamera();
        adjustPlayerCollider();
        velocity_last_frame = body.velocity;
        if (body.velocity.magnitude > 0) {
            if (move_state == movementState.RUN) {
                AudioController.audioPlayer.stepSound();
            }
        }
    }

    void punchCheck() {
        if (move_state == movementState.RUN) {
            RaycastHit hit_info;
            Ray facing = new Ray(transform.position - transform.forward, transform.forward);
            Debug.DrawRay(transform.position - transform.forward * 2, transform.forward, Color.blue, 2f);
            Debug.DrawRay(transform.position, transform.right, Color.green, 4f);
            knock_lock = true;
            if (Physics.SphereCast(facing, 1.1f, out hit_info, 2f, enemy_layer)) {
                victim = hit_info.rigidbody.gameObject;
                if (victim.GetComponent<Enemy>().current_state != EnemyState.KO &&
                        victim.GetComponent<Enemy>().current_state != EnemyState.BEING_FLIPPED) {
                            AudioController.audioPlayer.punchSound();
                    victim.GetComponent<Enemy>().getPunched();
                    Vector3 fist = (victim.transform.position - transform.position) * 50f;
                    victim.GetComponent<Rigidbody>().AddForceAtPosition(fist, victim.transform.position + victim.transform.up);
                }
            }
        }
    }

    void resolveGrab() {
        if (move_state == movementState.RUN) {
            body.velocity = Vector3.zero;
            RaycastHit hit_info;
            Ray facing = new Ray(transform.position - transform.forward, transform.forward);
            Debug.DrawRay(transform.position - transform.forward * 2, transform.forward, Color.blue, 2f);
            Debug.DrawRay(transform.position, transform.right, Color.green, 4f);

            if (Physics.SphereCast(facing, 1.1f, out hit_info, 2f, enemy_layer)) {
                victim = hit_info.rigidbody.gameObject;
                if (victim.GetComponent<Enemy>().current_state != EnemyState.KO &&
                        victim.GetComponent<Enemy>().current_state != EnemyState.BEING_FLIPPED && victim.GetComponent<Enemy>().current_state != EnemyState.DEAD) {
                    if (moveInput() && !body.isKinematic && !flip_lock)
                        throwEnemy();
                    else {
                        move_state = movementState.GRABBING;
                        victim.GetComponent<Enemy>().getGrabbed();
                    }
                }
            }
        } else if (move_state == movementState.GRABBING) {
            if (victim.GetComponent<Enemy>().current_state != EnemyState.GRABBED) {
                move_state = movementState.RUN;
            }
        }
    }

    void throwEnemy() {
        flip_lock = true;
        body.isKinematic = true;
        GetComponent<BoxCollider>().enabled = false;
        GetComponent<Rigidbody>().GetComponent<Rigidbody>().isKinematic = true;
        // Invoke("unlockFlip", 0.5f);
        victim.GetComponent<Enemy>().getFlipped();
    }

    public void unlockFlip() {
        flip_lock = false;
        body.isKinematic = false;
    }

    void unlockKnock() {
        knock_lock = false;
    }

    bool lockControlsIfNeeded() {
        if (flip_lock) {
            if (victim.GetComponent<Enemy>().current_state != EnemyState.KO &&
                    victim.GetComponent<Enemy>().current_state != EnemyState.BEING_FLIPPED) {
                victim.GetComponent<Enemy>().getFlipped();
            }
            return true;
        }

        if (under_obstacle_last_frame != playerIsUnderObstacle()) {
            control_lock_start_time = Time.time;
        }

        if (control_lock_start_time != 0f &&
                Time.time - control_lock_start_time < control_change_delay) {
            body.velocity = velocity_last_frame;
            return true;
        }
        return false;
    }

    bool moveInput() {
        return Input.GetKey(UP_KEY) || Input.GetKey(DOWN_KEY) || Input.GetKey(LEFT_KEY) || Input.GetKey(RIGHT_KEY);
    }

    void setVelocityFromInput() {
        if (flip_lock)
            return;

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
        } else if (move_state == movementState.GRABBING) {
            body.velocity = vel.normalized * grabbing_speed;
        } else if (move_state == movementState.AGAINST_WALL) {
            body.velocity = vel.normalized * against_wall_speed;
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
            } else if (move_state == movementState.AGAINST_WALL) {
                updateAgainstWall();
            }
        } else if (move_state != movementState.CRAWL && move_state != movementState.GRABBING) {
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

        if (move_state == movementState.GRABBING && body.velocity.magnitude > 0) {
            body.transform.forward = body.velocity.normalized * -1f;
        } else if (body.velocity != Vector3.zero) {
            gameObject.transform.forward = body.velocity.normalized;
            if (move_state == movementState.CRAWL) {
                body.transform.Rotate(new Vector3(90f, 0f, 0f));
            }
        }
    }

    void keepPlayerWithinPositionLimits() {
        Vector3 vel = body.velocity;
        if (!playerWithinXPositionLimits(vel)) {
            vel.x = 0;
        }
        if (!playerWithinZPositionLimits(vel)) {
            vel.z = 0;
        }
        body.velocity = vel;
    }

    void Knock() {
        GameObject[] all_Enemy = GameObject.FindGameObjectsWithTag("Enemy");
        float range = 20f;
        GameObject[] all_point = GameObject.FindGameObjectsWithTag("Waypoint");
        AudioController.audioPlayer.knockSound();
        foreach (GameObject point in all_point) {
            if (point.transform.position == transform.position)
                return;
        }

        GameObject current_player_point = Instantiate(waypoint_prefab, transform.position, Quaternion.identity) as GameObject;
        current_player_point.name = "player_pos" + poscount++;
        GameObject closest_enemy = gameObject;
        float closest_distance = range + 10f;
        foreach (GameObject grunt in all_Enemy) {
            bool is_this_even_possible = grunt.gameObject.GetComponent<Enemy>().current_state != EnemyState.KO && grunt.gameObject.GetComponent<Enemy>().current_state != EnemyState.BEING_FLIPPED;
            Vector3 to_player = grunt.transform.position - transform.position;
            if (is_this_even_possible && to_player.magnitude < range && to_player.magnitude < closest_distance) {
                closest_distance = to_player.magnitude;
                closest_enemy = grunt;
            }
        }

        if (closest_distance < range + 9f) {
            closest_enemy.GetComponent<Enemy>().investigate(current_player_point);
            current_player_point.GetComponent<PatrolPoint>().waiting = true;
        } else {
            current_player_point.GetComponent<PatrolPoint>().waiting = false;
        }

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
        } else if (!playerIsUnderObstacle()) {
            body.transform.position = new Vector3(body.transform.position.x, 1f, body.transform.position.z);
            body.transform.Rotate(new Vector3(-90f, 0f, 0f));
            move_state = movementState.RUN;
        } else
            AudioController.audioPlayer.cantSound();
    }

    void adjustCamera() {
        bool camera_moved = false;

        if (move_state == movementState.AGAINST_WALL) {
            camera_moved = moveCameraIfByWallEdge();
            if (!camera_moved) {
                camera_moved = moveCameraIfWallsOnBothSides();
            }
        } else if (move_state == movementState.CRAWL) {
            camera_moved = moveCameraIfUnderObstacle();
            under_obstacle_last_frame = camera_moved;
        }

        if (!camera_moved) {
            CameraController.cam_control.moveToOverviewPosition();
        }
    }

    bool moveCameraIfByWallEdge() {
        RaycastHit right_hit_info, left_hit_info;
        bool right_by_wall, left_by_wall;
        Vector3 right_check_pos = this.transform.position;
        Vector3 left_check_pos = this.transform.position;
        int layer_mask = 1 << LayerMask.NameToLayer("Obstacle");
        right_check_pos.y += 1.25f;
        left_check_pos.y += 1.25f;

        right_check_pos += this.transform.right * (1.5f + (this.transform.localScale.x / 2f));
        right_check_pos += this.transform.forward * -0.75f;
        left_check_pos += this.transform.right * -(1.5f + (this.transform.localScale.x / 2f));
        left_check_pos += this.transform.forward * -0.75f;

        Debug.DrawRay(this.transform.position, right_check_pos - this.transform.position);
        Debug.DrawRay(this.transform.position, left_check_pos - this.transform.position);
        right_by_wall = !Physics.CheckSphere(right_check_pos, 0.01f, layer_mask);
        left_by_wall = !Physics.CheckSphere(left_check_pos, 0.01f, layer_mask);
        Physics.Raycast(right_check_pos, this.transform.right * -1f, out right_hit_info, 2f, layer_mask);
        Physics.Raycast(left_check_pos, this.transform.right, out left_hit_info, 2f, layer_mask);
        if (right_by_wall && left_by_wall && playerByWallEdge(true) && playerByWallEdge(false)) {
            if (right_hit_info.distance < left_hit_info.distance)
                CameraController.cam_control.moveToLookDownHallway(this.transform, right_hit_info.distance, true);
            else
                CameraController.cam_control.moveToLookDownHallway(this.transform, left_hit_info.distance, false);
            return true;
        } else if (right_by_wall && playerByWallEdge(true)) {
            CameraController.cam_control.moveToLookDownHallway(this.transform, right_hit_info.distance, true);
            return true;
        } else if (left_by_wall && playerByWallEdge(false)) {
            CameraController.cam_control.moveToLookDownHallway(this.transform, left_hit_info.distance, false);
            return true;
        }
        return false;
    }

    bool playerByWallEdge(bool right_side) {
        Vector3 check_pos = this.transform.position;
        int layer_mask = 1 << LayerMask.NameToLayer("Obstacle");
        int direction = 1;
        if (!right_side) {
            direction = -1;
        }
        check_pos.y += 1.25f;

        check_pos += this.transform.right * (1.5f + (this.transform.localScale.x / 2f)) * direction;
        check_pos += this.transform.forward * -2.5f;

        if (!Physics.CheckSphere(check_pos, 0.01f, layer_mask)) {
            return true;
        }
        return false;
    }

    bool moveCameraIfWallsOnBothSides() {
        RaycastHit right_hit_info, left_hit_info;
        bool right_wall, left_wall;
        Vector3 right_check_pos = this.transform.position;
        Vector3 left_check_pos = this.transform.position;
        int layer_mask = 1 << LayerMask.NameToLayer("Obstacle");

        right_check_pos += this.transform.right * (1.0f + (this.transform.localScale.x / 2f));
        left_check_pos += this.transform.right * -(1.0f + (this.transform.localScale.x / 2f));

        right_wall = Physics.CheckSphere(right_check_pos, 0.01f, layer_mask);
        left_wall = Physics.CheckSphere(left_check_pos, 0.01f, layer_mask);
        Debug.DrawRay(this.transform.position, right_check_pos - this.transform.position);
        Debug.DrawRay(this.transform.position, left_check_pos - this.transform.position);
        Physics.Raycast(this.transform.position, this.transform.right, out right_hit_info, 2f, layer_mask);
        Physics.Raycast(this.transform.position, this.transform.right * -1, out left_hit_info, 2f, layer_mask);
        if (right_wall && left_wall) {
            print(right_hit_info.distance + " " + left_hit_info.distance);
            if (right_hit_info.distance > left_hit_info.distance)
                CameraController.cam_control.moveToLookDownHallway(this.transform, right_hit_info.distance, true);
            else
                CameraController.cam_control.moveToLookDownHallway(this.transform, left_hit_info.distance, false);
            return true;
        }
        return false;

    }

    bool moveCameraIfUnderObstacle() {
        if (playerIsUnderObstacle()) {
            CameraController.cam_control.moveToUnderObstacle(this.transform);
            return true;
        }
        return false;
    }

    bool playerIsUnderObstacle() {
        bool hit;
        RaycastHit hit_info;

        hit = Physics.Raycast(this.transform.position, this.transform.forward * -1f, out hit_info);
        if (move_state == movementState.CRAWL && hit && hit_info.collider.gameObject.tag == "Obstacle") {
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

        if (under_obstacle_last_frame) {
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

    void initPositionLimits() {
        GameObject ground_game_object = GameObject.FindGameObjectWithTag("Ground");
        x_position_min = ground_game_object.transform.position.x -
                (ground_game_object.transform.localScale.x / 2f) + 0.1f;
        x_position_max = ground_game_object.transform.position.x +
                (ground_game_object.transform.localScale.x / 2f) - 0.1f;
        z_position_min = ground_game_object.transform.position.z -
                (ground_game_object.transform.localScale.z / 2f) + 0.3f;
        z_position_max = ground_game_object.transform.position.z +
                (ground_game_object.transform.localScale.z / 2f) - 0.3f;
    }

    bool playerWithinXPositionLimits(Vector3 movement_direction) {
        float mid_offset = this.transform.localScale.x / 2f;
        if (this.transform.position.x - mid_offset < x_position_min && movement_direction.x < 0) {
            return false;
        }
        if (this.transform.position.x + mid_offset > x_position_max && movement_direction.x > 0) {
            return false;
        }
        return true;
    }

    bool playerWithinZPositionLimits(Vector3 movement_direction) {
        float mid_offset = this.transform.localScale.z / 2f;
        if (this.transform.position.z - mid_offset < z_position_min && movement_direction.z < 0) {
            return false;
        }
        if (this.transform.position.z + mid_offset > z_position_max && movement_direction.z > 0) {
            return false;
        }
        return true;
    }

    public void spawnPlayer() {
        spawning_player = true;
        spawn_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spawn_sphere.GetComponent<Renderer>().material = spawn_sphere_material;
        Destroy(spawn_sphere.GetComponent<SphereCollider>());
        spawn_sphere.transform.position = this.transform.position;
        spawn_sphere.transform.localScale = Vector3.zero;
        spawn_start_time = Time.time;
    }

    public void unspawnPlayer() {
        unspawning_player = true;
        spawn_sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spawn_sphere.GetComponent<Renderer>().material = spawn_sphere_material;
        Destroy(spawn_sphere.GetComponent<SphereCollider>());
        spawn_sphere.transform.position = this.transform.position;
        spawn_sphere.transform.localScale = spawn_sphere_max_size;
        spawn_start_time = Time.time;
    }

    void growSpawnSphere() {
        if (spawn_sphere.transform.localScale == spawn_sphere_max_size) {
            Destroy(spawn_sphere);
            spawning_player = false;
            CameraController.cam_control.playerSpawned();
            return;
        }

        spawn_sphere.transform.localScale = Vector3.Lerp(Vector3.zero, spawn_sphere_max_size,
            (Time.time - spawn_start_time) / spawn_duration);
    }

    void shrinkSpawnSphere() {
        if (spawn_sphere.transform.localScale == Vector3.zero) {
            Destroy(spawn_sphere);
            unspawning_player = false;
            CameraController.cam_control.startNextLevel();
            return;
        }

        spawn_sphere.transform.localScale = Vector3.Lerp(spawn_sphere_max_size, Vector3.zero,
            (Time.time - spawn_start_time) / unspawn_duration);
    }

    void OnTriggerEnter(Collider coll) {
        if (coll.gameObject.tag == "EndPoint" && CameraController.cam_control.playerHasControl()) {
            CameraController.cam_control.endLevel();
        }
    }
}
