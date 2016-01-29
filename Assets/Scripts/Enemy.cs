using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum EnemyState {
    PATROL,
    SEE_PLAYER,
    INVESTIGATE,
    BEING_FLIPPED,
    KO
};

public class Enemy : MonoBehaviour {
    public GameObject player;
    public float speed = 10f;
    public float detect_range = 5f; // Max detection range
    public float detect_angle = 30f;
    public List<GameObject> search_path;
    List<GameObject> possible_path;
    public Color default_color;
    public Color surprised_color = Color.yellow;
    public GameObject waypoint_prefab;
    public GameObject current_patrol_point;
    GameObject original_patrol_point;
    public GameObject investigate_point;
    GameObject patrol_return_point;
    GameObject old_prp;
    Vector3 last_position;
    public LayerMask player_and_walls;

    bool ____________________;  // Divider for the inspector

    public EnemyState current_state;
    EnemyState former_state;
    float misc_counter;
    Rigidbody body;
    NavMeshAgent mesh_agent;
    LineRenderer detect_area_parimeter;
    int detect_parimeter_arc_vertices = 10;
    int detect_parimeter_total_vertices = 12;

    // Use this for initialization
    void Start() {
        body = gameObject.GetComponent<Rigidbody>();
        mesh_agent = gameObject.GetComponent<NavMeshAgent>();
        default_color = this.GetComponent<Renderer>().material.color;

        detect_area_parimeter = gameObject.GetComponent<LineRenderer>();
        detect_area_parimeter.SetVertexCount(detect_parimeter_total_vertices);

        current_state = EnemyState.PATROL;
        search_path=new List<GameObject>();
        possible_path=new List<GameObject>();

    }

    // Update is called once per frame
    void Update() {

        if (current_state == EnemyState.BEING_FLIPPED) {
            flyProgress();
        }
        else if (current_state == EnemyState.KO) {
           Invoke("getUp",5f);
        }
        else drawDetectParimeter();
        if (current_state == EnemyState.PATROL) {
            patrolUpdate();
        }
        else if (current_state == EnemyState.SEE_PLAYER) {
            seePlayerUpdate();
        }
        else if (current_state == EnemyState.INVESTIGATE) {
            bool new_investigation = search_path.Count == 0 && investigate_point.GetComponent<PatrolPoint>().announced;
            bool case_closed =investigate_point==old_prp;
            if (new_investigation||case_closed) {
                float shortest = 9999999;
                float current_dist=0;
                possible_path.Clear();
                possible_path=new List<GameObject>();
                patrol_return_point.GetComponent<PatrolPoint>().neighbors.RemoveAll(x => x.point==null);
                findRoute(patrol_return_point, ref shortest, ref current_dist);
                if (case_closed)
                    investigate_point=null;
                search_path.Remove(search_path.FirstOrDefault());
                current_patrol_point=search_path.FirstOrDefault();
            }
            patrolUpdate();
        }

    }
    public void getFlipped() {
        former_state=current_state;
        current_state=EnemyState.BEING_FLIPPED;
        body.isKinematic=true;
        misc_counter=0;
        transform.rotation = player.transform.rotation;
        transform.position=player.transform.position+player.transform.forward;
        mesh_agent.Stop();
    }
    void flyProgress() {
        transform.RotateAround(player.transform.position, player.transform.right, 10f);
        if (misc_counter++ > 12) {
            misc_counter=0;
            current_state=EnemyState.KO;
            player.GetComponent<Rigidbody>().isKinematic = false;
        }
    }
    void getUp() {
        mesh_agent.Resume();
    }

    void patrolUpdate() {
        // Move along patrol route
        if (current_patrol_point!=null)
            mesh_agent.destination = current_patrol_point.transform.position;

        // Check if player can now be seen
        if (playerInFieldOfView()) {
            detectPlayer();
        }
    }

    void seePlayerUpdate() {
        // Stop patrol
        mesh_agent.destination = this.transform.position;

        if (!playerInFieldOfView()) {
            Invoke("undetectPlayer", 0.1f);
        }
    }

    bool playerInFieldOfView() {
        RaycastHit see_player;
        Vector3 to_player = MovementController.player.transform.position - transform.position;
        Physics.Raycast(this.transform.position, to_player, out see_player, player_and_walls);
        if (Vector3.Angle(this.transform.forward, to_player) < detect_angle &&
                see_player.collider.gameObject.tag == "Player" && see_player.distance < detect_range) {
            Debug.DrawRay(this.transform.position, to_player);
            return true;
        }
        return false;
    }

    void drawDetectParimeter() {
        int cur_vertex = 0;
        float current_angle = -detect_angle;
        float angle_delta = (detect_angle * 2) / (detect_parimeter_arc_vertices - 1);
        bool hit;
        Vector3 arc_offset;
        Vector3 arc_vertex;
        RaycastHit ray_hit;

        detect_area_parimeter.SetPosition(cur_vertex++, gameObject.transform.position);
        for (int i = 0; i < detect_parimeter_arc_vertices; ++i) {
            arc_offset = Quaternion.Euler(0, current_angle, 0) * gameObject.transform.forward * detect_range;
            hit = Physics.Raycast(this.transform.position, arc_offset, out ray_hit, detect_range);
            if (hit && ray_hit.collider.gameObject.tag == "Obstacle") {
                arc_vertex = ray_hit.point;
            }
            else {
                arc_vertex = gameObject.transform.position + arc_offset;
            }

            detect_area_parimeter.SetPosition(cur_vertex++, arc_vertex);
            current_angle += angle_delta;
        }
        detect_area_parimeter.SetPosition(cur_vertex, gameObject.transform.position);
    }

    void detectPlayer() {
        if (playerInFieldOfView()) {
            current_state = EnemyState.SEE_PLAYER;
            this.GetComponent<Renderer>().material.color = surprised_color;
        }
    }

    void undetectPlayer() {
        if (!playerInFieldOfView()) {
            current_state = EnemyState.PATROL;
            if (search_path.Count!=0)
                current_state = EnemyState.INVESTIGATE;
            this.GetComponent<Renderer>().material.color = default_color;
        }
    }

    public void setPatrolPoint(GameObject new_patrol_point) {
        if (current_state == EnemyState.PATROL)
            current_patrol_point = new_patrol_point;
        else if (current_state == EnemyState.INVESTIGATE) {
            GameObject next_point=search_path.FirstOrDefault();
            if (current_patrol_point == next_point) {
                search_path.Remove(next_point);
                if (search_path.Count()==0&& current_patrol_point==investigate_point) {
                    resume_patrol();
                    return;
                }
                if (search_path.Count()==0) {
                    current_state=EnemyState.PATROL;
                    current_patrol_point = original_patrol_point;
                    patrol_return_point.GetComponent<PatrolPoint>().waiting=false;
                    return;
                }
                current_patrol_point = search_path.FirstOrDefault();
                current_patrol_point.GetComponent<PatrolPoint>().in_use=false;
            }
        }
    }



    public void investigate(GameObject new_point) {
        if (current_state!=EnemyState.INVESTIGATE) {
            current_state=EnemyState.INVESTIGATE;
            investigate_point=new_point;
            patrol_return_point = Instantiate(waypoint_prefab, transform.position, Quaternion.identity) as GameObject;
            patrol_return_point.name="last_pos_of_"+this.gameObject.name;
            patrol_return_point.GetComponent<PatrolPoint>().in_use=true;
            original_patrol_point=current_patrol_point;
        }
        else {
            if (investigate_point!=null)
                investigate_point.GetComponent<PatrolPoint>().waiting=false;
            investigate_point=new_point;
            search_path.Clear();
        }
    }
    public void resume_patrol() {
        old_prp=patrol_return_point;
        investigate_point.GetComponent<PatrolPoint>().waiting=false;
        investigate_point=old_prp;
        patrol_return_point = Instantiate(waypoint_prefab, transform.position, Quaternion.identity) as GameObject;
        patrol_return_point.name="last_pos_of_"+this.gameObject.name;
        patrol_return_point.GetComponent<PatrolPoint>().in_use=true;
    }

    void findRoute(GameObject current_path_point, ref float shortest, ref float current_dist) {
        current_path_point.GetComponent<PatrolPoint>().in_use= true;
        current_path_point.GetComponent<PatrolPoint>().neighbors.RemoveAll(x => x.point==null);
        List<Neighbor> debug_N_list= current_path_point.GetComponent<PatrolPoint>().neighbors.ToList();
        possible_path.Add(current_path_point);
        if (current_path_point==investigate_point) {
            shortest =current_dist;
            search_path=possible_path.ToList();
            possible_path.Remove(current_path_point);
            return;
        }
        foreach (Neighbor next in current_path_point.GetComponent<PatrolPoint>().neighbors) {
            if (possible_path.Contains(next.point))
                continue;
            if (next.point!=investigate_point&&!next.point.GetComponent<PatrolPoint>().start)
                continue;
            current_dist+=next.distance;
            if (current_dist<shortest) {
                Debug.DrawLine(current_path_point.transform.position, next.point.transform.position, Color.red, 1f);
                findRoute(next.point, ref shortest, ref current_dist);
            }
            current_dist-=next.distance;
        }
        possible_path.Remove(current_path_point);
        current_path_point.GetComponent<PatrolPoint>().in_use= false;

    }

}
