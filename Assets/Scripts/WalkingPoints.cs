using UnityEngine;
using System.Collections;

public class WalkingPoints : MonoBehaviour {
    public GameObject next_point;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnTriggerEnter(Collider coll) {
        if (coll.gameObject.tag == "Enemy") {
            coll.gameObject.GetComponent<EnemyBehavior>().setNext(next_point);
        }
    }
}
