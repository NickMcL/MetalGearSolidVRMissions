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

    public Material line_material;
    public int detect_area_total_vertices = 12;
    public float detect_area_line_width;
    GameObject[] detect_area_objects;
    LineRenderer[] detect_area_lines;
    Color detect_area_color = new Color(110f, 218f, 218f);

    // Use this for initialization
    void Start() {
        body = gameObject.GetComponent<Rigidbody>();
        mesh_agent = gameObject.GetComponent<NavMeshAgent>();
        default_color = this.GetComponent<Renderer>().material.color;

        initDetectArea();

        current_state = EnemyState.PATROL;
        search_path=new List<GameObject>();
        possible_path=new List<GameObject>();
    }

    // Update is called once per frame
    void Update() {

        if (current_state == EnemyState.BEING_FLIPPED) {
            flyProgress();
        } else if (current_state == EnemyState.KO) {
            getUp();
        } else {
            drawDetectArea();
        }

        if (current_state == EnemyState.PATROL) {
            patrolUpdate();
        } else if (current_state == EnemyState.SEE_PLAYER) {
            seePlayerUpdate();
        } else if (current_state == EnemyState.INVESTIGATE) {
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
    }
    void flyProgress() {
        transform.RotateAround(player.transform.right, player.transform.right *-1f, 10f);
        if (misc_counter++ > 12)
            misc_counter=0;
            current_state=EnemyState.KO;
    }
    void getUp() {

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

    void initDetectArea() {
        Color line_color_start = detect_area_color;
        Color line_color_end = detect_area_color;
        line_color_start.a = 255f;
        line_color_end.a = 0f;
        detect_area_objects = new GameObject[detect_area_total_vertices];
        detect_area_lines = new LineRenderer[detect_area_total_vertices];

        for (int i = 0; i < detect_area_total_vertices; ++i) {
            detect_area_objects[i] = new GameObject();
            detect_area_objects[i].layer = LayerMask.NameToLayer("Radar");
            detect_area_objects[i].transform.parent = this.transform;
            detect_area_lines[i] = detect_area_objects[i].AddComponent<LineRenderer>();
            detect_area_lines[i].SetVertexCount(2);
            detect_area_lines[i].SetWidth(
                0, (2f * Mathf.PI * detect_range * ((detect_angle * 2) / 360f)) / detect_area_total_vertices);
            detect_area_lines[i].material = line_material;
            detect_area_lines[i].SetColors(line_color_start, line_color_end);
        }
    }

    void drawDetectArea() {
        float current_angle = -detect_angle;
        float angle_delta = (detect_angle * 2) / detect_area_total_vertices;
        Vector3 radar_lift = new Vector3(0f, 15f, 0f);
        Vector3 range_scale = new Vector3(detect_range, detect_range, detect_range);
        Vector3 arc_offset;

        for (int i = 0; i < detect_area_total_vertices; ++i) {
            detect_area_lines[i].SetPosition(0, this.transform.position + radar_lift);
            arc_offset = Vector3.Scale(Quaternion.AngleAxis(current_angle, Vector3.up) *
                this.transform.forward, range_scale) + radar_lift;
            detect_area_lines[i].SetPosition(1, this.transform.position + arc_offset);
            current_angle += angle_delta;
        }
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
