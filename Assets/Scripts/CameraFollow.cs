using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour {
    public GameObject player;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (CameraController.cam_control.shouldFollowPlayer()) {
            gameObject.transform.position = player.transform.position;
        }
	}
}
