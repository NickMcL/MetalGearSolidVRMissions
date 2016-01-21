using UnityEngine;
using System.Collections;

public class PatrolPoint : MonoBehaviour {
    public GameObject next_patrol_point;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnTriggerEnter(Collider coll) {
        if (coll.gameObject.tag == "Enemy") {
            coll.gameObject.GetComponent<Enemy>().setPatrolPoint(next_patrol_point);
        }
    }
}
