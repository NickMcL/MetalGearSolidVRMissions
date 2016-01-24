using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PatrolPoint : MonoBehaviour {
    public GameObject next_patrol_point;
    public List<GameObject> neighbors;
    public bool start;
    public int waiting = 0;

    public LayerMask waypoints_and_walls;
    // Use this for initialization
    void Start() {
        GameObject[] all_points= GameObject.FindGameObjectsWithTag("Waypoint");
        List<GameObject> pos_neighbors = new List<GameObject>();
        Vector3 this_point = transform.position;
        float range = 50f;

        foreach (GameObject point in all_points) {
            if (point ==this.gameObject)
                continue;
            Vector3 to_point = point.transform.position-transform.position;
            Vector3.Normalize(to_point);
            RaycastHit Hit;
            Debug.DrawRay(this_point, to_point);
            if (Physics.Raycast(this_point, to_point, out Hit, range, waypoints_and_walls))
                if (Hit.collider.gameObject==point)
                    pos_neighbors.Add(point);
        }
        neighbors=pos_neighbors;

    }

    // Update is called once per frame
    void Update() {

    }
    void forget_friend (GameObject friend){
        neighbors.Remove(friend);
    }
    void OnTriggerEnter(Collider coll) {
        if (coll.gameObject.tag == "Enemy") {
            coll.gameObject.GetComponent<Enemy>().setPatrolPoint(next_patrol_point);
            if (!start) {
                if (waiting>0)
                    waiting--;
                else {
                    foreach (GameObject neighbor in neighbors) {
                        neighbor.GetComponent<PatrolPoint>().forget_friend(this.gameObject);
                    }
                    Destroy(this);
                }
                    
            }
        }
    }
}
