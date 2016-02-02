using UnityEngine;
using System.Collections;

public class SelectorBox : MonoBehaviour {
    public static SelectorBox box;
    public GameObject selector_game_object;

    public float trans_duration;
    float trans_start_time;
    Vector3 current_location;
    Vector3 next_location;
    bool start_position_set;

    void Awake() {
        box = this;
        start_position_set = false;
    }

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        if (!start_position_set) {
            return;
        }

        if (current_location != next_location) {
            selector_game_object.transform.position = Vector3.Lerp(
                    current_location, next_location,
                    (Time.time - trans_start_time) / trans_duration);
            if (selector_game_object.transform.position == next_location) {
                current_location = next_location;
            }
        }
	}

    public void setStartPosition(Vector3 pos) {
        selector_game_object.transform.position = pos;
        current_location = pos;
        next_location = pos;
        start_position_set = true;
    }

    public void moveSelectorBox(Vector3 pos) {
        current_location = selector_game_object.transform.position;
        next_location = pos;
        trans_start_time = Time.time;
    }
}
