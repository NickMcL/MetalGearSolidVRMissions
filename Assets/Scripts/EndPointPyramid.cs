using UnityEngine;
using System.Collections;

public class EndPointPyramid : MonoBehaviour {
    public static EndPointPyramid end_point;

    public float pyramid_height = 2f;
    public float pyramid_base_width = 0.75f;
    public int pyramid_total_cubes = 20;
    public float rotation_duration = 1f;
    public Material pyramid_material;

    GameObject[] pyramid_cubes;
    float rotate_start_time;
    bool pyramid_created;

    void Awake() {
        end_point = this;
        pyramid_cubes = new GameObject[pyramid_total_cubes];
        pyramid_created = false;
    }

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
        if (pyramid_created) {
            rotatePyramid();	
        }
	}

    public void createPyramid(Vector3 position) {
        EnemyFollow pyramid_radar_point = 
            GameObject.FindGameObjectWithTag("EndPointTrack").GetComponent<EnemyFollow>();
        Vector3 current_pos = position;
        float cube_height = pyramid_height / pyramid_total_cubes;
        Vector3 current_scale = new Vector3(pyramid_base_width, cube_height, pyramid_base_width);
        float size_delta = pyramid_base_width / pyramid_total_cubes;

        for (int i = 0; i < pyramid_total_cubes; ++i) {
            pyramid_cubes[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pyramid_cubes[i].tag = "EndPoint";
            pyramid_cubes[i].layer = LayerMask.NameToLayer("EndPoint");
            pyramid_cubes[i].GetComponent<BoxCollider>().isTrigger = true;
            pyramid_cubes[i].GetComponent<Renderer>().material = pyramid_material;

            pyramid_cubes[i].transform.position = current_pos;
            pyramid_cubes[i].transform.localScale = current_scale;
            current_pos.y -= cube_height;
            current_scale.x -= size_delta;
            current_scale.z -= size_delta;
        }
        pyramid_radar_point.enemy = pyramid_cubes[0].gameObject;
        pyramid_created = true;
        rotate_start_time = Time.time;
    }

    public void destroyPyramid() {
        for (int i = 0; i < pyramid_total_cubes; ++i) {
            Destroy(pyramid_cubes[i]);
        }
        pyramid_created = false;
    }

    void rotatePyramid() {
        Quaternion cube_rotation;
        float lerp_point = (Time.time - rotate_start_time) / rotation_duration;
        bool reset_time = false;
        if (lerp_point > 1f) {
            lerp_point -= 1f;
            reset_time = true;
        }
        cube_rotation = Quaternion.Euler(Vector3.Lerp(Vector3.zero, Vector3.up * 360f, lerp_point) * -1f);

        foreach (GameObject cube in pyramid_cubes) {
            cube.transform.rotation = cube_rotation;
        }
        if (reset_time) {
            rotate_start_time = Time.time;
        }
    }
}
