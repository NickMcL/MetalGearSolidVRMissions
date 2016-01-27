using UnityEngine;
using System.Collections;

public class TiltCameraArea : MonoBehaviour {
    void OnTriggerEnter(Collider coll) {
        if (coll.gameObject.tag == "Player") {
            CameraController.cam_control.tiltCameraUp();
        }
    }

    void OnTriggerExit(Collider coll) {
        if (coll.gameObject.tag == "Player") {
            CameraController.cam_control.moveToDefaultPosition();
        }
    }
}
