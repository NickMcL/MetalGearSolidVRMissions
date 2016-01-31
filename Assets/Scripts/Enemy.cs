using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum EnemyState {
    PATROL,
    SEE_PLAYER,
    INVESTIGATE,
    GRABBED,
    BEING_FLIPPED,
    KO,
    GETTINGUP,
    SEARCHING,
    PATROL_RETURN
};

public class Enemy : MonoBehaviour {
    GameObject player;
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
    GameObject temp_waypoint;
    public LayerMask player_and_walls;

    bool ____________________;  // Divider for the inspector

    public EnemyState current_state;
    EnemyState former_state;
    float misc_counter = 0;
    float search_stage = 0;
    float misc_counter2 = 0;
    float ko_count = 1;
    float ko_min = 1;
    Rigidbody body;
    NavMeshAgent mesh_agent;
    bool change_course = false;
    bool looking = false;
    Quaternion look_target;

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
        search_path = new List<GameObject>();
        possible_path = new List<GameObject>();
    }

    void Awake() {
        GameObject[] maybe_player = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject possible_player in maybe_player) {
            if (possible_player.name == "Player") {
                player = possible_player;
                break;
            }
        }
    }

    // Update is called once per frame
    void Update() {
        drawDetectArea();

        if (current_state == EnemyState.BEING_FLIPPED) {
            flyProgress();
            return;
        } else if (current_state == EnemyState.KO) {
            misc_counter += Time.deltaTime;
            if (misc_counter > 0.35) {
                Vector3 down = new Vector3(0, -100, 0);
                body.AddForceAtPosition(down, transform.position);
            }

            if (misc_counter > 0.1) {
                player.GetComponent<MovementController>().unlockFlip();
            }

            if (misc_counter > ko_count) {
                getUp();
            }
            return;
        } else if (current_state == EnemyState.GETTINGUP) {
            getUp();
        } else if (current_state == EnemyState.GRABBED) {
            updateGrab();
        }

        if (ko_count > ko_min) {
            if (misc_counter > 2) {
                misc_counter = 0;
                ko_count = ko_min;
            }
            misc_counter += Time.deltaTime;
        } else if (ko_count < ko_min) {
            ko_count = ko_min;
        }

        if (current_state == EnemyState.PATROL) {
            patrolUpdate();
        } else if (current_state == EnemyState.SEE_PLAYER) {
            seePlayerUpdate();
        } else if (current_state == EnemyState.INVESTIGATE) {
            bool new_investigation = search_path.Count == 0 && investigate_point.GetComponent<PatrolPoint>().announced;
            if (new_investigation) {
                planPath();
                return;
            }

            if (investigate_point == current_patrol_point && investigate_point.GetComponent<PatrolPoint>().announced) {
                RaycastHit Hit;
                Vector3 to_point = investigate_point.transform.position - transform.position;
                if (Physics.Raycast(transform.position, to_point, out Hit, 2f)) {
                    investigate_point.GetComponent<PatrolPoint>().triggered(gameObject.GetComponent<BoxCollider>());
                }
            }
            patrolUpdate();
        } else if (current_state == EnemyState.PATROL_RETURN) {
            if (search_path.Count == 0) {
                resume_patrol();
            }
            patrolUpdate();
        } else if (current_state == EnemyState.SEARCHING) {
            searching();
        }
    }

    void planPath() {
        float shortest = 9999999;
        float current_dist = 0;
        possible_path.Clear();
        possible_path = new List<GameObject>();
        temp_waypoint.GetComponent<PatrolPoint>().neighbors.RemoveAll(x => x.point == null);
        findRoute(temp_waypoint, ref shortest, ref current_dist);
        temp_waypoint.GetComponent<PatrolPoint>().waiting = false;
        if (current_state == EnemyState.PATROL_RETURN) {
            investigate_point = null;
        }
        search_path.Remove(search_path.FirstOrDefault());
        current_patrol_point = search_path.FirstOrDefault();
        temp_waypoint = null;
    }

    void saveStatus() {
        former_state = current_state;
        if (search_path.Count == 0) {
            original_patrol_point = current_patrol_point;
        }
    }

    public void getFlipped() {
        mesh_agent.updateRotation = false;
        mesh_agent.updatePosition = false;
        body.isKinematic = true;
        body.freezeRotation = false;
        mesh_agent.destination = this.transform.position;
        //       player.GetComponent<Rigidbody>().isKinematic = true;
        saveStatus();
        current_state = EnemyState.BEING_FLIPPED;
        /// body.isKinematic=true;
        misc_counter = 0;
        //transform.rotation = player.transform.rotation;
        //transform.position = player.transform.position + player.transform.forward;
    }

    public void getGrabbed() {
        mesh_agent.updateRotation = false;
        mesh_agent.updatePosition = false;
        body.freezeRotation = false;
        //  body.isKinematic = true;
        saveStatus();
        current_state = EnemyState.GRABBED;
        misc_counter = 0;
    }

    void getKOd() {
        current_state = EnemyState.KO;
        misc_counter = 0;
        misc_counter2 = 0;
        if (ko_count == ko_min) {
            ko_count += 3;
        }
    }

    void getUp() {
        if (current_state == EnemyState.KO) {
            current_state = EnemyState.GETTINGUP;
            misc_counter = 0;
        }
        misc_counter += Time.deltaTime;

        if (mesh_agent.updateRotation == false) {
            if (Vector3.Magnitude(transform.up - Vector3.up) < 0.5f) {
                Vector3 fullstop = new Vector3(0, 0, 0);
                mesh_agent.Warp(transform.position);
                body.velocity = fullstop;
                mesh_agent.updateRotation = true;
            }
            body.AddForceAtPosition(Vector3.up * 800 * Time.deltaTime, transform.position + transform.up);
        } else if (Mathf.Abs(body.velocity.magnitude) < 0.01f) {
            mesh_agent.updatePosition = true;
            body.freezeRotation = true;
            misc_counter = 0;
            misc_counter2 = 0;
            current_state = EnemyState.SEARCHING;
        }
    }

    void updateGrab() {
        if (misc_counter2 > 0.03) {
            misc_counter2 = 0;
            Vector3 p_forward = player.transform.forward * -1f;
            Vector3 diag = p_forward + player.transform.up;
            Quaternion diag_dir = Quaternion.LookRotation(diag);
            diag = Vector3.Cross(transform.forward, diag);
            body.AddTorque(diag * 60f);
            diag = p_forward * -1f + player.transform.up;
            diag = Vector3.Cross(transform.up, diag);
            body.AddTorque(diag * 100f);
            Vector3 neck = (transform.position + transform.up * 0.8f);
            Vector3 force_infront = (player.transform.position + p_forward * 0.8f + player.transform.up * 0.5f) - (neck);
            float exp_force = 1.7f;

            if (force_infront.magnitude > 1.2) {
                exp_force = force_infront.magnitude * force_infront.magnitude * force_infront.magnitude;
                if (exp_force > 5) {
                    exp_force = 5;
                }
            } else if (force_infront.magnitude < 0.05) {
                exp_force = force_infront.magnitude * force_infront.magnitude;
                body.velocity = body.velocity * 0.5f;
            }

            float innacuracy = Vector3.Magnitude(force_infront.normalized - body.velocity.normalized);
            if (innacuracy > 1) {
                body.velocity = body.velocity * 0.5f;
            }
            body.AddForceAtPosition(force_infront * exp_force * 120f, neck);
        }

        if (misc_counter > 4) {
            getKOd();
            released();
        }
        misc_counter += Time.deltaTime;
        misc_counter2 += Time.deltaTime;
    }

    void searching() {
        if (!looking) {
            looking = true;
            search_stage = 0;
            misc_counter2 = 0;
            search_stage = 0;
            mesh_agent.updateRotation = false;
            mesh_agent.updatePosition = false;
            body.freezeRotation = false;
        }

        if (misc_counter2 > 1) {
            search_stage++;
            misc_counter2 = 0;
        }
        if (search_stage == 0) {
            look_target.SetLookRotation(transform.forward * -1 + transform.right,transform.up);
            search_stage++;
        }
        if (search_stage == 2) {
            look_target.SetLookRotation(transform.right *-1, transform.up);
            search_stage++;
        }
        if (search_stage == 4) {
            look_target.SetLookRotation(transform.right * -1, transform.up);
            search_stage++;
        }
        if (search_stage == 6) {
            look_target.SetLookRotation(transform.right, transform.up);
            search_stage++;
        }
        if (Quaternion.Angle(transform.rotation, look_target) < 5) {
            misc_counter2 += Time.deltaTime;
        } else {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look_target, Time.deltaTime * 160);
        }
        if (search_stage > 7) {
            search_stage = 0;
            misc_counter2 = 0;
            looking = false;
            
           // transform.rotation =  Quaternion.FromToRotation(transform.up, Vector3.up);
           // transform.rotation = Quaternion.AngleAxis(0, Vector3.forward);
           // transform.rotation = Quaternion.AngleAxis(0, Vector3.right);
            mesh_agent.Warp(transform.position);
            mesh_agent.destination = transform.position;
            mesh_agent.updateRotation = true;
            mesh_agent.updatePosition = true;
            body.freezeRotation = true;
            resume_patrol();
        }
    }

    void released() {
        misc_counter = 0;
        misc_counter2 = 0;
        if (current_state == EnemyState.KO) {
            return;
        }
    }

    void flyProgress() {
        if (misc_counter < 0.1f) {
            Vector3 posy = transform.position;
            posy.y += 12f * Time.deltaTime;
            transform.position = posy;
        } else {
            body.isKinematic = false;
            Vector3 push = (transform.up + player.transform.forward * -1f) * 300f;
            //  Vector3 push = transform.up * 200;
            Vector3 target = transform.position;
            target.y += 1;
            body.AddForceAtPosition(push, target);
            body.AddTorque(player.transform.right * -2000f);
            getKOd();
            return;
        }
        misc_counter += Time.deltaTime;
    }

    void patrolUpdate() {
        // Move along patrol route
        if (current_patrol_point != null) {
            mesh_agent.destination = current_patrol_point.transform.position;
        }

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
            if (search_path.Count != 0)
                current_state = EnemyState.INVESTIGATE;
            this.GetComponent<Renderer>().material.color = default_color;
        }
    }

    public void setPatrolPoint(GameObject new_patrol_point) {
        if (current_state == EnemyState.PATROL) {
            current_patrol_point = new_patrol_point;
        } else if (search_path.Count > 0) {
            GameObject next_point = search_path.FirstOrDefault();
            if (current_patrol_point == next_point) {
                search_path.Remove(next_point);
                if (search_path.Count() == 0) {
                    resume_patrol();
                    return;
                }
                current_patrol_point = search_path.FirstOrDefault();
            }
        }
    }

    public void investigate(GameObject new_point) {
        if (current_state != EnemyState.INVESTIGATE && current_state != EnemyState.PATROL_RETURN) {
            current_state = EnemyState.INVESTIGATE;
            original_patrol_point = current_patrol_point;
        } else if (investigate_point != null) {
            investigate_point.GetComponent<PatrolPoint>().waiting = false;
        }

        investigate_point = new_point;
        temp_waypoint = Instantiate(waypoint_prefab, transform.position, Quaternion.identity) as GameObject;
        temp_waypoint.name = "temp_pos" + this.gameObject.name;
        search_path.Clear();
    }

    public void resume_patrol() {
        if (current_state != EnemyState.PATROL_RETURN && current_state != EnemyState.PATROL) {
            search_path.Clear();
            current_state = EnemyState.PATROL_RETURN;
            if (investigate_point != null) {
                investigate_point.GetComponent<PatrolPoint>().waiting = false;
            }
            investigate_point = original_patrol_point;
            temp_waypoint = Instantiate(waypoint_prefab, transform.position, Quaternion.identity) as GameObject;
            temp_waypoint.name = "last_pos_of_" + this.gameObject.name;
            return;
        }
        if (temp_waypoint != null)
            planPath();
        else {
            current_state = EnemyState.PATROL;
        }
    }

    void findRoute(GameObject current_path_point, ref float shortest, ref float current_dist) {
        current_path_point.GetComponent<PatrolPoint>().neighbors.RemoveAll(x => x.point == null);
        List<Neighbor> debug_N_list = current_path_point.GetComponent<PatrolPoint>().neighbors.ToList();
        possible_path.Add(current_path_point);
        if (current_path_point == investigate_point) {
            shortest = current_dist;
            search_path = possible_path.ToList();
            possible_path.Remove(current_path_point);
            return;
        }
        foreach (Neighbor next in current_path_point.GetComponent<PatrolPoint>().neighbors) {
            if (possible_path.Contains(next.point))
                continue;
            if (next.point != investigate_point && !next.point.GetComponent<PatrolPoint>().start)
                continue;
            current_dist += next.distance;
            if (current_dist < shortest) {
                Debug.DrawLine(current_path_point.transform.position, next.point.transform.position, Color.red, 1f);
                findRoute(next.point, ref shortest, ref current_dist);
            }
            current_dist -= next.distance;
        }
        possible_path.Remove(current_path_point);
    }
}
