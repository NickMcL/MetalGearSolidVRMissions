using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct Neighbor {
    public GameObject point;
    public float distance;
    //Waypoint object, Distance

    public Neighbor(GameObject p, float d) {
        point = p;
        distance=d;
    }
};
public class PatrolPoint : MonoBehaviour {
    public GameObject next_patrol_point;
    public List<GameObject> N_view;
    public List<Neighbor> neighbors;
    public bool start;
    public bool waiting = true;
    public bool announced;
    public LayerMask waypoints_and_walls;
    public bool in_use;
    public bool dead = false;

    // Use this for initialization
    void Start() {
        in_use=false;
        dead = false;
    }
    void Awake(){
        GameObject[] all_points= GameObject.FindGameObjectsWithTag("Waypoint");
        Vector3 this_point = transform.position;
        float range = 50f;
        announced=false;
        neighbors = new List<Neighbor>();
        N_view = new List<GameObject>();
        foreach (GameObject point in all_points) {
            if (point ==this.gameObject)
                continue;
            Vector3 to_point = point.transform.position-transform.position;
            // Vector3.Normalize(to_point);
            RaycastHit Hit;
            Debug.DrawRay(this_point, to_point);
            if (Physics.Raycast(this_point, to_point, out Hit, range, waypoints_and_walls))
                if (Hit.collider.gameObject==point) {
                    Neighbor current_neighbor=new Neighbor(point, Hit.distance);
                    neighbors.Add(current_neighbor);

                }

        }
        foreach (Neighbor neighbor in neighbors) {
            N_view.Add(neighbor.point);
        }
    }

    // Update is called once per frame
    void Update() {
        if (!dead) {
            if (!waiting && !start)
                cease_exist();
            if (announced==false && start==false) {
                neighbors.RemoveAll(x => x.point==null);
                foreach (Neighbor neighbor in neighbors) {
                    Neighbor me=new Neighbor(this.gameObject, neighbor.distance);
                    neighbor.point.GetComponent<PatrolPoint>().neighbors.Add(me);
                    neighbor.point.GetComponent<PatrolPoint>().neighbors.Reverse();
                }
                announced=true;
            }
        }
    }
    void forget_friend(GameObject friend) {
        neighbors.RemoveAll(x => x.point==null);
        neighbors.RemoveAll(x => x.point==friend);

    }
    void cease_exist() {
        if (!in_use) {

            Vector3 down = transform.position;
            down.y =-20;
            transform.position=down;
            neighbors.RemoveAll(x => x.point==null);
            foreach (Neighbor neighbor in neighbors) {

                neighbor.point.GetComponent<PatrolPoint>().forget_friend(this.gameObject);
            }
            dead = true;
            Invoke("final_destroy", 1f);
        }
    }
    void final_destroy() {
        Destroy(this.gameObject);
    }
    //want to mage both triggers call a function rather than have same code twice
    void OnTriggerEnter(Collider coll) {
        triggered(coll);
    }
    void OnTriggerStay(Collider coll) {
        triggered(coll);
    }
    public void triggered(Collider coll) {
        if (coll.gameObject.tag == "Enemy"&&coll.GetComponent<Enemy>().current_patrol_point==this.gameObject) {
            coll.gameObject.GetComponent<Enemy>().setPatrolPoint(next_patrol_point);
            if (!start&& !in_use) {
                if (waiting)
                    waiting=false;
                else
                    cease_exist();
            }
        }
    }
}
