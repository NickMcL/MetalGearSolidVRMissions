using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {
    Vector3 OVERVIEW_START_POS = new Vector3(0f, 5.71f, -13.72f);
    Vector3 OVERVIEW_START_ROT = new Vector3(56.1f, 0f, 0f);
    Vector3 OVERVIEW_TILT_POS = new Vector3(0f, 5.71f, -5.8f);
    Vector3 OVERVIEW_TILT_ROT = new Vector3(66.7f, 0f, 0f);

    const float CAMERA_START_FOV = 46f;

    public static CameraController cam_control;  // Singleton
    public Camera main_camera;

    public enum CameraState {
        DEFAULT,
        TILTED,
        DOWN_HALLWAY,
        UNDER_OBSTACLE
    };
    public CameraState camera_state;
    bool in_tilt_area;
    bool moved_under_obstacle;

    Vector3 start_camera_pos;
    Vector3 start_camera_rot;
    Vector3 next_camera_pos;
    Vector3 next_camera_rot;
    float lerp_start_time;
    public float transition_time = 1.0f;

	void Start () {
        cam_control = this;
        main_camera = this.gameObject.GetComponent<Camera>();
        main_camera.enabled = true;
        main_camera.fieldOfView = CAMERA_START_FOV;
        in_tilt_area = false;
        moved_under_obstacle = false;

        main_camera.transform.localPosition = OVERVIEW_START_POS;
        main_camera.transform.localEulerAngles = OVERVIEW_START_ROT;
        start_camera_pos = OVERVIEW_START_POS;
        start_camera_rot = OVERVIEW_START_ROT;
        next_camera_pos = OVERVIEW_START_POS;
        next_camera_rot = OVERVIEW_START_ROT;
        camera_state = CameraState.DEFAULT;
	}

    void Update() {
        Vector3 lerp;
        if (start_camera_pos != next_camera_pos) {
            lerp = Vector3.Lerp(start_camera_pos, next_camera_pos,
                    ((Time.time - lerp_start_time) / transition_time));
            if (camera_state == CameraState.DOWN_HALLWAY) {
                main_camera.transform.position = lerp;
            } else {
                main_camera.transform.localPosition = lerp;
            }
        }
        if (start_camera_rot != next_camera_rot) {
            main_camera.transform.eulerAngles = Vector3.Lerp(start_camera_rot, next_camera_rot,
                    ((Time.time - lerp_start_time) / transition_time));
        }

        if (camera_state == CameraState.UNDER_OBSTACLE && 
                ((Time.time - lerp_start_time) / transition_time) >= 1.0f) {
            moved_under_obstacle = true; 
        }
	}

    public void moveToDefaultPosition() {
        start_camera_pos = main_camera.transform.localPosition;
        start_camera_rot = main_camera.transform.eulerAngles;
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
        start_camera_rot = main_camera.transform.eulerAngles;
        next_camera_pos = OVERVIEW_TILT_POS;
        next_camera_rot = OVERVIEW_TILT_ROT;
        lerp_start_time = Time.time;
        camera_state = CameraState.TILTED;
    }

    public void moveToLookDownHallway(Transform player_transform, bool right_side) {
        if (camera_state == CameraState.DOWN_HALLWAY) {
            return;
        }

        Vector3 side_movement = player_transform.right;
        if (!right_side) {
            side_movement *= -1;
        }

        start_camera_pos = main_camera.transform.position;
        start_camera_rot = main_camera.transform.eulerAngles;
        next_camera_pos = player_transform.position + (player_transform.forward * 6f) + (side_movement * 4f);
        next_camera_rot = Quaternion.LookRotation(player_transform.forward * -1f).eulerAngles;
        lerp_start_time = Time.time;
        camera_state = CameraState.DOWN_HALLWAY;
    }

    public void moveToUnderObstacle(Transform player_transform) {
        if (camera_state == CameraState.UNDER_OBSTACLE) {
            if (moved_under_obstacle) {
                rotateWithPlayer(player_transform);
            }
            return;
        }

        start_camera_pos = main_camera.transform.localPosition;
        start_camera_rot = main_camera.transform.eulerAngles;
        next_camera_pos = player_transform.up * player_transform.localScale.y;
        next_camera_rot = Quaternion.LookRotation(player_transform.up).eulerAngles;
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
        return camera_state != CameraState.DOWN_HALLWAY;
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
