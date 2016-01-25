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
     
        public override bool Equals(object obj) {
            return obj is Neighbor && this == (Neighbor)obj;
        }
        public override int GetHashCode() {
            return point.GetHashCode() ^ distance.GetHashCode();
        }
        public static bool operator ==(Neighbor x, Neighbor y) {
            return x.point == y.point;
        }
        public static bool operator !=(Neighbor x, Neighbor y) {
            return !(x == y);
        }
     

    };
public class PatrolPoint : MonoBehaviour {
    public GameObject next_patrol_point;
    public List<GameObject> N_view;
    public List<Neighbor> neighbors;
    public bool start;
    public int waiting = 0;

    public LayerMask waypoints_and_walls;



    // Use this for initialization
    void Start() {
        GameObject[] all_points= GameObject.FindGameObjectsWithTag("Waypoint");
        Vector3 this_point = transform.position;
        float range = 50f;
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
                    if (start==false)
                        point.GetComponent<PatrolPoint>().add_friend(this.gameObject, Hit.distance);
                }

        }
        foreach (Neighbor neighbor in neighbors) {
            N_view.Add(neighbor.point);
        }
    }

    // Update is called once per frame
    void Update() {
        if (waiting==0)
            cease_exist();
    }
    void add_friend(GameObject friend, float distance) {
        Neighbor current_neighbor=new Neighbor(friend, distance);
        neighbors.Add(current_neighbor);
    }
    void forget_friend(Neighbor friend) {
        neighbors.Remove(friend);
    }
    void cease_exist() {
         foreach (Neighbor neighbor in neighbors) {
                        Neighbor me = new Neighbor(this.gameObject, neighbor.distance);
                        neighbor.point.GetComponent<PatrolPoint>().forget_friend(me);
                    }
                    Destroy(this);
                
    }
    void OnTriggerEnter(Collider coll) {
        if (coll.gameObject.tag == "Enemy") {
            coll.gameObject.GetComponent<Enemy>().setPatrolPoint(next_patrol_point);
            if (!start) {
                if (waiting>0)
                    waiting--;
                else
                    cease_exist();
                   

            }
        }
    }
}
