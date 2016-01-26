using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {
    Vector3 OVERVIEW_START_POS = new Vector3(0f, 5.71f, -13.72f);
    Vector3 OVERVIEW_START_ROT = new Vector3(56.1f, 0f, 0f);
    Vector3 OVERVIEW_TILT_POS = new Vector3(0f, 5.71f, -5.8f);
    Vector3 OVERVIEW_TILT_ROT = new Vector3(66.7f, 0f, 0f);

    const float OVERVIEW_START_FOV = 46f;

    public static CameraController overview_camera;  // Singleton
    public Camera zoom_camera;  // Camera used for the enviroments shifts

    Vector3 start_overview_pos;
    Vector3 start_overview_rot;
    Vector3 next_overview_pos;
    Vector3 next_overview_rot;
    float lerp_start_time;
    public float transition_time = 1.0f;

	void Start () {
        overview_camera = this;
        this.gameObject.GetComponent<Camera>().fieldOfView = OVERVIEW_START_FOV;

        this.transform.localPosition = OVERVIEW_START_POS;
        this.transform.localEulerAngles = OVERVIEW_START_ROT;
        start_overview_pos = OVERVIEW_START_POS;
        start_overview_rot = OVERVIEW_START_ROT;
        next_overview_pos = OVERVIEW_START_POS;
        next_overview_rot = OVERVIEW_START_ROT;
	}

    void Update() {
        if (start_overview_pos != next_overview_pos) {
            this.transform.localPosition = Vector3.Lerp(start_overview_pos, next_overview_pos,
                    ((Time.time - lerp_start_time) / transition_time));
        }
        if (start_overview_rot != next_overview_rot) {
            this.transform.localEulerAngles = Vector3.Lerp(start_overview_rot, next_overview_rot,
                    ((Time.time - lerp_start_time) / transition_time));
        }
	}

    public void moveToDefaultPosition() {
        start_overview_pos = this.transform.localPosition;
        start_overview_rot = this.transform.localEulerAngles;
        next_overview_pos = OVERVIEW_START_POS;
        next_overview_rot = OVERVIEW_START_ROT;
        lerp_start_time = Time.time;
    }

    public void tiltCameraUp() {
        start_overview_pos = this.transform.localPosition;
        start_overview_rot = this.transform.localEulerAngles;
        next_overview_pos = OVERVIEW_TILT_POS;
        next_overview_rot = OVERVIEW_TILT_ROT;
        lerp_start_time = Time.time;
    }
}
