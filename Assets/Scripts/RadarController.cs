using UnityEngine;
using System.Collections;

public class RadarController : MonoBehaviour {
    public GUISkin radar_gui_skin;
    Camera radar_camera;

	// Use this for initialization
	void Start () {
        radar_camera = this.gameObject.GetComponent<Camera>();
    }
	
	// Update is called once per frame
	void Update () {
        if (MovementController.player.under_obstacle_last_frame) {
            radar_camera.enabled = false;
        } else {
            radar_camera.enabled = true;
        }
	}

    void OnGUI() {
        if (radar_camera.enabled) {
            GUI.skin = radar_gui_skin;
            GUI.Box(new Rect(radar_camera.pixelRect.x, (Screen.height - radar_camera.pixelRect.yMax),
                radar_camera.pixelWidth, radar_camera.pixelHeight), "");
        }
    }
}
