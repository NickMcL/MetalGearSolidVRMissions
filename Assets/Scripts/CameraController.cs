using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {
    Vector3 OVERVIEW_START_POS = new Vector3(0f, 5.71f, -13.72f);
    Quaternion OVERVIEW_START_ROT = Quaternion.Euler(56.1f, 0f, 0f);
    Vector3 OVERVIEW_TILT_POS = new Vector3(0f, 5.71f, -5.8f);
    Quaternion OVERVIEW_TILT_ROT = Quaternion.Euler(66.7f, 0f, 0f);
    Vector3 HALLWAY_ROT_ANGLES = new Vector3(0f, 15f, 0f);

    const float CAMERA_START_FOV = 46f;

    public static CameraController cam_control;  // Singleton
    public Camera main_camera;

    public enum CameraState {
        DEFAULT,
        TILTED,
        DOWN_HALLWAY_RIGHT,
        DOWN_HALLWAY_LEFT,
        UNDER_OBSTACLE,
    };
    public CameraState camera_state;
    bool in_tilt_area;
    bool moved_under_obstacle;

    Vector3 start_camera_pos;
    Quaternion start_camera_rot;
    Vector3 next_camera_pos;
    Quaternion next_camera_rot;
    float lerp_start_time;
    public float transition_time = 0.25f;

	void Start () {
        cam_control = this;
        main_camera = this.gameObject.GetComponent<Camera>();
        main_camera.enabled = true;
        main_camera.fieldOfView = CAMERA_START_FOV;
        in_tilt_area = false;
        moved_under_obstacle = false;

        main_camera.transform.localPosition = OVERVIEW_START_POS;
        main_camera.transform.rotation = OVERVIEW_START_ROT;
        start_camera_pos = OVERVIEW_START_POS;
        start_camera_rot = OVERVIEW_START_ROT;
        next_camera_pos = OVERVIEW_START_POS;
        next_camera_rot = OVERVIEW_START_ROT;
        camera_state = CameraState.DEFAULT;
	}

    void Update() {
        Vector3 lerp;
        if (moved_under_obstacle) {
            return;
        }

        if (start_camera_pos != next_camera_pos) {
            lerp = Vector3.Lerp(start_camera_pos, next_camera_pos,
                    ((Time.time - lerp_start_time) / transition_time));
            if (shouldFollowPlayer()) {
                main_camera.transform.localPosition = lerp;
            } else {
                main_camera.transform.position = lerp;
            }
        }
        if (start_camera_rot != next_camera_rot) {
            main_camera.transform.rotation = Quaternion.Lerp(
                    start_camera_rot, next_camera_rot,
                    ((Time.time - lerp_start_time) / transition_time));
        }

        if (camera_state == CameraState.UNDER_OBSTACLE && 
                ((Time.time - lerp_start_time) / transition_time) >= 1.0f) {
            moved_under_obstacle = true; 
        }
	}

    public void moveToDefaultPosition() {
        start_camera_pos = main_camera.transform.localPosition;
        start_camera_rot = main_camera.transform.rotation;
        next_camera_pos = OVERVIEW_START_POS;
        next_camera_rot = OVERVIEW_START_ROT;
        lerp_start_time = Time.time;
        in_tilt_area = false;
        camera_state = CameraState.DEFAULT;
    }

    public void tiltCameraUp() {
        in_tilt_area = true;
        if (!cameraInOverviewPosition()) {
            return;
        }

        start_camera_pos = main_camera.transform.localPosition;
        start_camera_rot = main_camera.transform.rotation;
        next_camera_pos = OVERVIEW_TILT_POS;
        next_camera_rot = OVERVIEW_TILT_ROT;
        lerp_start_time = Time.time;
        camera_state = CameraState.TILTED;
    }

    public void moveToLookDownHallway(Transform player_transform, float offset, bool right_side) {
        if (right_side && camera_state == CameraState.DOWN_HALLWAY_RIGHT ||
                !right_side && camera_state == CameraState.DOWN_HALLWAY_LEFT) {
            return;
        }

        Vector3 side_movement = player_transform.right;
        Vector3 look_back = Quaternion.LookRotation(player_transform.forward * -1f).eulerAngles;
        Vector3 look_offset = HALLWAY_ROT_ANGLES;
        if (!right_side) {
            side_movement *= -1;
            look_offset *= -1;
            camera_state = CameraState.DOWN_HALLWAY_LEFT;
        } else {
            camera_state = CameraState.DOWN_HALLWAY_RIGHT;
        }

        start_camera_pos = main_camera.transform.position;
        start_camera_rot = main_camera.transform.rotation;
        next_camera_pos = player_transform.position +
            (player_transform.forward * 6f) + (player_transform.up * -0.0f) + 
            (side_movement * 5f + side_movement * -offset);
        next_camera_rot = Quaternion.Euler(look_back + look_offset);
        lerp_start_time = Time.time;
    }

    public void moveToUnderObstacle(Transform player_transform) {
        if (camera_state == CameraState.UNDER_OBSTACLE) {
            if (moved_under_obstacle) {
                rotateWithPlayer(player_transform);
            }
            return;
        }

        start_camera_pos = main_camera.transform.localPosition;
        start_camera_rot = main_camera.transform.rotation;
        next_camera_pos = player_transform.up * player_transform.localScale.y;
        next_camera_rot = Quaternion.LookRotation(player_transform.up);
        lerp_start_time = Time.time;
        camera_state = CameraState.UNDER_OBSTACLE;
        moved_under_obstacle = false;
    }

    public void moveToOverviewPosition() {
        if (cameraInOverviewPosition()) {
            return;
        }

        moved_under_obstacle = false;
        if (in_tilt_area) {
            camera_state = CameraState.DEFAULT;
            tiltCameraUp();
        } else {
            moveToDefaultPosition();
        }
    }

    public bool shouldFollowPlayer() {
        return camera_state != CameraState.DOWN_HALLWAY_LEFT &&
            camera_state != CameraState.DOWN_HALLWAY_RIGHT;
    }

    bool cameraInOverviewPosition() {
        return camera_state == CameraState.DEFAULT || camera_state == CameraState.TILTED;
    }

    void rotateWithPlayer(Transform player_transform) {
        float scaling_factor = player_transform.localScale.y / 2f;
        float angle = player_transform.eulerAngles.y;
        if (angle > 180f) {
            angle -= 180f;
        }
        scaling_factor += (Mathf.Abs(90f - angle) / 90f) * scaling_factor;
        
        main_camera.transform.localPosition = player_transform.up * scaling_factor;
        main_camera.transform.rotation = Quaternion.LookRotation(player_transform.up);
    }
}
