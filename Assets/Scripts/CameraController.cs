using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;


public class CameraController : MonoBehaviour {
    Dictionary<string, string> START_LEVEL_TEXT = new Dictionary<string, string>() {
        {SceneNames.LEVEL_1, "SNEAKING  MODE\nNO  WEAPON  LEVEL  1\n\nDon't  be  seen  by  enemy  soldiers  and\nhead  for  the  goal!   " +
            "This  training\nmission  is  aborted  if  you  are\nspotted." },
        {SceneNames.LEVEL_2, "SNEAKING  MODE\nNO  WEAPON  LEVEL  2\n\nUse  the  Radar  effectively  and  head\n" +
            "for  the  goal!   There  is  a  blind  spot\nbehind  the  enemy." },
        {SceneNames.LEVEL_3, "SNEAKING  MODE\nNO  WEAPON  LEVEL  3\n\nUse  shortcuts  wisely  and  head  for\n" +
            "the  goal!   Infiltrate  by  crawling\nthrough  low  ceiling  areas." },
        {SceneNames.CUSTOM_NM, "SNEAKING  MODE\nNO  WEAPON  CUSTOM  LEVEL  1\n\nCarefully  but  quickly  navigate\naround  the  guards  " +
            "and  head  for  the\ngoal!   Try   to  lure  guards  away  from\ntight  areas." },
        {SceneNames.CUSTOM_AJ, "SNEAKING  MODE\nNO  WEAPON  CUSTOM  LEVEL  2\n\nUse  all  of  the  skills  at  your\n" +
            "disposal  and  head  for  the  goal!\nRemember  that  you  can  flip,  punch,\nor  grab  troublesome  guards."},
    };

    Dictionary<string, string> NEXT_LEVEL = new Dictionary<string, string>() {
        {SceneNames.LEVEL_1, SceneNames.LEVEL_2 },
        {SceneNames.LEVEL_2, SceneNames.LEVEL_3 },
        {SceneNames.LEVEL_3, SceneNames.CUSTOM_NM },
        {SceneNames.CUSTOM_NM, SceneNames.CUSTOM_AJ },
        {SceneNames.CUSTOM_AJ, null },
    };

    Dictionary<string, Vector3> LEVEL_END_POINT_POSITIONS = new Dictionary<string, Vector3>() {
        {SceneNames.LEVEL_1, new Vector3(7.5f, 2f, 11f) },
        {SceneNames.LEVEL_2, new Vector3(10f, 2f, 10f) },
        {SceneNames.LEVEL_3, new Vector3(10f, 2f, 9f) },
        {SceneNames.CUSTOM_NM, new Vector3(11.5f, 2f, -21f) },
        {SceneNames.CUSTOM_AJ, new Vector3(73f, 2f, -13f) },
    };

    Dictionary<string, Vector3> LEVEL_END_CAMERA_SCALE = new Dictionary<string, Vector3>() {
        {SceneNames.LEVEL_1, new Vector3(1f, 0f, 1f) },
        {SceneNames.LEVEL_2, new Vector3(1f, 0f, 1f) },
        {SceneNames.LEVEL_3, new Vector3(1f, 0f, 1f) },
        {SceneNames.CUSTOM_NM, new Vector3(1f, 0f, -1f) },
        {SceneNames.CUSTOM_AJ, new Vector3(1f, 0f, -1f) },
    };

    Dictionary<string, Vector3> PLAYER_SPAWN_POSITIONS = new Dictionary<string, Vector3>() {
        {SceneNames.LEVEL_1, new Vector3(-1.5f, 1f, -3.5f) },
        {SceneNames.LEVEL_2, new Vector3(-10f, 1f, -10f) },
        {SceneNames.LEVEL_3, new Vector3(-4f, 1f, -9f) },
        {SceneNames.CUSTOM_NM, new Vector3(-22.5f, 1f, -23f) },
        {SceneNames.CUSTOM_AJ, new Vector3(2f, 1f, 0f) },
    };

    Dictionary<string, float> START_PAN_HEIGHT = new Dictionary<string, float>() {
        {SceneNames.LEVEL_1, 15f },
        {SceneNames.LEVEL_2, 20f },
        {SceneNames.LEVEL_3, 20f },
        {SceneNames.CUSTOM_NM, 25f },
        {SceneNames.CUSTOM_AJ, 40f },
    };

    Dictionary<string, float> START_PAN_DURATION = new Dictionary<string, float>() {
        {SceneNames.LEVEL_1, 0.75f },
        {SceneNames.LEVEL_2, 0.75f },
        {SceneNames.LEVEL_3, 0.75f },
        {SceneNames.CUSTOM_NM, 0.75f },
        {SceneNames.CUSTOM_AJ, 1.5f },
    };

    Vector3 START_LEVEL_POS_OFFSET = new Vector3(0f, 1f, -2.75f);
    Quaternion START_LEVEL_ROT = Quaternion.Euler(30f, 0f, 0f);
    Vector3 END_LEVEL_POS_OFFSET = new Vector3(4f, -1f, 4f);

    Quaternion START_MID_ROT = Quaternion.Euler(70f, 0f, 0f);

    Vector3 OVERVIEW_START_POS = new Vector3(0f, 5.71f, -13.72f);
    Quaternion OVERVIEW_START_ROT = Quaternion.Euler(56.1f, 0f, 0f);
    Vector3 OVERVIEW_TILT_POS = new Vector3(0f, 5.71f, -5.8f);
    Quaternion OVERVIEW_TILT_ROT = Quaternion.Euler(66.7f, 0f, 0f);
    Vector3 HALLWAY_ROT_ANGLES = new Vector3(0f, 15f, 0f);

    const float CAMERA_START_FOV = 46f;
    const float PAUSE_LIGHT_INTENSITY = 0.1f;
    const float UNPAUSE_LIGHT_INTENSITY = 1.0f;

    const KeyCode START_KEY = KeyCode.Return;

    public static CameraController cam_control;  // Singleton
    public Camera main_camera;
    public GUISkin start_level_gui_skin;
    public float end_level_delay = 2.0f;
    public bool game_paused;

    public enum CameraState {
        DEFAULT,
        TILTED,
        DOWN_HALLWAY_RIGHT,
        DOWN_HALLWAY_LEFT,
        UNDER_OBSTACLE,
        START_LEVEL,
        START_PAN_OVERVIEW,
        START_PAN_PLAYER,
        END_LEVEL
    };
    public CameraState camera_state;
    bool in_tilt_area;
    bool moved_under_obstacle;
    bool spawning_player;

    Vector3 start_camera_pos;
    Quaternion start_camera_rot;
    Vector3 next_camera_pos;
    Quaternion next_camera_rot;
    float lerp_start_time;
    public float end_level_start_time;
    string current_level;
    string current_start_level_text;

    public float in_level_trans_time = 0.25f;

	void Start () {
        cam_control = this;
        main_camera = this.gameObject.GetComponent<Camera>();
        main_camera.enabled = true;
        main_camera.fieldOfView = CAMERA_START_FOV;
        in_tilt_area = false;
        moved_under_obstacle = false;
        spawning_player = false;
        game_paused = false;
        current_start_level_text = "";
        current_level = SceneManager.GetActiveScene().name;

        MissionFailed.current_level_scene_name = current_level;
        LevelFinish.current_level_scene_name = current_level;
        LevelFinish.next_level_scene_name = NEXT_LEVEL[current_level];
        if (NEXT_LEVEL[current_level] == null) {
            LevelFinish.last_level = true;
        } else {
            LevelFinish.last_level = false;
        }

        startLevel(current_level);
	}

    void Update() {
        Vector3 lerp;
        float transition_time = getTransitionTime();
        if (playerHasControl() && Input.GetKeyDown(START_KEY) && !game_paused) {
            pause();
            return;
        }
        if (moved_under_obstacle || spawning_player) {
            return;
        }
        if (camera_state == CameraState.START_LEVEL && Input.anyKey) {
            cameraStartPanMid();
            return;
        }
        if (camera_state == CameraState.END_LEVEL &&
                ((Time.time - end_level_start_time) > end_level_delay)) {
            spawning_player = true;
            setSeePlayer(false);
            MovementController.player.unspawnPlayer();
            return;
        }

        if (start_camera_pos != next_camera_pos) {
            lerp = Vector3.Lerp(start_camera_pos, next_camera_pos,
                    ((Time.time - lerp_start_time) / transition_time));
            if (shouldFollowPlayer()) {
                main_camera.transform.localPosition = lerp;
            } else {
                main_camera.transform.position = lerp;
            }
        }
        if (start_camera_rot != next_camera_rot) {
            main_camera.transform.rotation = Quaternion.Lerp(
                    start_camera_rot, next_camera_rot,
                    ((Time.time - lerp_start_time) / transition_time));
        }

        if (camera_state == CameraState.UNDER_OBSTACLE &&
                ((Time.time - lerp_start_time) / transition_time) >= 1.0f) {
            moved_under_obstacle = true;
        } else if (camera_state == CameraState.START_PAN_OVERVIEW &&
            ((Time.time - lerp_start_time) / transition_time) >= 1.0f) {
            cameraStartPanPlayer();
        } else if (camera_state == CameraState.START_PAN_PLAYER &&
             ((Time.time - lerp_start_time) / transition_time) >= 1.0f) {
            spawning_player = true;
            MovementController.player.spawnPlayer();
        }
	}

    float getTransitionTime() {
        if (camera_state == CameraState.START_PAN_OVERVIEW ||
                camera_state == CameraState.START_PAN_PLAYER) {
            return START_PAN_DURATION[current_level];
        }
        else {
            return in_level_trans_time;
        }
    }

    void cameraStartPanMid() {
        GameObject ground =  GameObject.FindGameObjectWithTag("Ground");
        Vector3 mid_pos = ground.transform.position;
        mid_pos.y = START_PAN_HEIGHT[current_level];

        start_camera_pos = main_camera.transform.position;
        start_camera_rot = main_camera.transform.rotation;
        next_camera_pos = mid_pos;
        next_camera_rot = START_MID_ROT;

        lerp_start_time = Time.time;
        camera_state = CameraState.START_PAN_OVERVIEW;
    }

    void cameraStartPanPlayer() {
        MovementController.player.transform.position = PLAYER_SPAWN_POSITIONS[current_level];
        GameObject player_follow =  GameObject.FindGameObjectWithTag("PlayerTrack");
        player_follow.GetComponent<CameraFollow>().transform.position =
            PLAYER_SPAWN_POSITIONS[current_level];
        this.transform.parent = player_follow.transform;
        moveToDefaultPosition();
        camera_state = CameraState.START_PAN_PLAYER;
    }

    void setSeePlayer(bool see_player) {
        if (see_player) {
            main_camera.cullingMask |= 1 << LayerMask.NameToLayer("Player");
        } else {
            main_camera.cullingMask &= ~(1 << LayerMask.NameToLayer("Player"));
        }
    }

    public void moveToDefaultPosition() {
        start_camera_pos = main_camera.transform.localPosition;
        start_camera_rot = main_camera.transform.rotation;
        next_camera_pos = OVERVIEW_START_POS;
        next_camera_rot = OVERVIEW_START_ROT;
        lerp_start_time = Time.time;
        in_tilt_area = false;
        camera_state = CameraState.DEFAULT;
    }

    public void tiltCameraUp() {
        in_tilt_area = true;
        if (!cameraInOverviewPosition()) {
            return;
        }

        start_camera_pos = main_camera.transform.localPosition;
        start_camera_rot = main_camera.transform.rotation;
        next_camera_pos = OVERVIEW_TILT_POS;
        next_camera_rot = OVERVIEW_TILT_ROT;
        lerp_start_time = Time.time;
        camera_state = CameraState.TILTED;
    }

    public void moveToLookDownHallway(Transform player_transform, float offset, bool right_side) {
        if (right_side && camera_state == CameraState.DOWN_HALLWAY_RIGHT ||
                !right_side && camera_state == CameraState.DOWN_HALLWAY_LEFT) {
            return;
        }

        Vector3 side_movement = player_transform.right;
        Vector3 look_back = Quaternion.LookRotation(player_transform.forward * -1f).eulerAngles;
        Vector3 look_offset = HALLWAY_ROT_ANGLES;
        if (!right_side) {
            side_movement *= -1;
            look_offset *= -1;
            camera_state = CameraState.DOWN_HALLWAY_LEFT;
        } else {
            camera_state = CameraState.DOWN_HALLWAY_RIGHT;
        }

        start_camera_pos = main_camera.transform.position;
        start_camera_rot = main_camera.transform.rotation;
        next_camera_pos = player_transform.position +
            (player_transform.forward * 6f) + (player_transform.up * -0.0f) + 
            (side_movement * 5f + side_movement * -offset);
        next_camera_rot = Quaternion.Euler(look_back + look_offset);
        lerp_start_time = Time.time;
    }

    public void moveToUnderObstacle(Transform player_transform) {
        if (camera_state == CameraState.UNDER_OBSTACLE) {
            if (moved_under_obstacle) {
                rotateWithPlayer(player_transform);
            }
            return;
        }

        start_camera_pos = main_camera.transform.localPosition;
        start_camera_rot = main_camera.transform.rotation;
        next_camera_pos = player_transform.up * player_transform.localScale.y;
        next_camera_rot = Quaternion.LookRotation(player_transform.up);
        lerp_start_time = Time.time;
        camera_state = CameraState.UNDER_OBSTACLE;
        moved_under_obstacle = false;
    }

    public void moveToOverviewPosition() {
        if (cameraInOverviewPosition()) {
            return;
        }

        moved_under_obstacle = false;
        if (in_tilt_area) {
            camera_state = CameraState.DEFAULT;
            tiltCameraUp();
        } else {
            moveToDefaultPosition();
        }
    }

    public bool shouldFollowPlayer() {
        return camera_state != CameraState.DOWN_HALLWAY_LEFT &&
            camera_state != CameraState.DOWN_HALLWAY_RIGHT;
    }

    public bool playerHasControl() {
        return !(camera_state == CameraState.START_LEVEL ||
            camera_state == CameraState.START_PAN_OVERVIEW ||
            camera_state == CameraState.START_PAN_PLAYER ||
            camera_state == CameraState.END_LEVEL);
    }

    public void playerSpawned() {
        setSeePlayer(true);
        spawning_player = false;
        camera_state = CameraState.DEFAULT;
    }

    bool cameraInOverviewPosition() {
        return camera_state == CameraState.DEFAULT || camera_state == CameraState.TILTED;
    }

    void rotateWithPlayer(Transform player_transform) {
        float scaling_factor = player_transform.localScale.y / 2f;
        float angle = player_transform.eulerAngles.y;
        if (angle > 180f) {
            angle -= 180f;
        }
        scaling_factor += (Mathf.Abs(90f - angle) / 90f) * scaling_factor;
        
        main_camera.transform.localPosition = player_transform.up * scaling_factor;
        main_camera.transform.rotation = Quaternion.LookRotation(player_transform.up);
    }

    void startLevel(string level_name) {
        Vector3 end_point_pos = LEVEL_END_POINT_POSITIONS[level_name];
        EndPointPyramid.end_point.createPyramid(end_point_pos);
        setSeePlayer(false);

        this.transform.parent = null;
        start_camera_pos = end_point_pos + START_LEVEL_POS_OFFSET;
        start_camera_rot = START_LEVEL_ROT;
        next_camera_pos = start_camera_pos;
        next_camera_rot = start_camera_rot;
        main_camera.transform.position = start_camera_pos;
        main_camera.transform.rotation = start_camera_rot;
        camera_state = CameraState.START_LEVEL;

        spawning_player = false;
        current_level = level_name;
    }

    public void endLevel() {
        Vector3 end_point_pos = LEVEL_END_POINT_POSITIONS[current_level];
        Vector3 camera_scale = LEVEL_END_CAMERA_SCALE[current_level];
        float end_level_rot;
        if (camera_scale.x == -1 && camera_scale.z == -1)
            end_level_rot = 45;
        else if (camera_scale.x == -1 && camera_scale.z == 1)
            end_level_rot = 135;
        else if (camera_scale.x == 1 && camera_scale.z == 1)
            end_level_rot = 225;
        else
            end_level_rot = 315;

        this.transform.parent = null;
        start_camera_pos = main_camera.transform.position;
        start_camera_rot = main_camera.transform.rotation;
        next_camera_pos = end_point_pos +
            Vector3.Scale(END_LEVEL_POS_OFFSET, LEVEL_END_CAMERA_SCALE[current_level]);
        next_camera_rot = Quaternion.Euler(0f, end_level_rot, 0f);
        main_camera.transform.position = start_camera_pos;
        main_camera.transform.rotation = start_camera_rot;
        camera_state = CameraState.END_LEVEL;
        lerp_start_time = Time.time;
        end_level_start_time = lerp_start_time;
        AudioController.audioPlayer.missionCompleteSound();
    }

    public void startNextLevel() {
        SceneManager.LoadScene(SceneNames.LEVEL_FINISH);
    }

    public void pause() {
        GameObject.FindGameObjectWithTag("Light").GetComponent<Light>().intensity = PAUSE_LIGHT_INTENSITY;
        this.gameObject.GetComponent<PauseMenu>().openPauseMenu();
        main_camera.cullingMask |= 1 << LayerMask.NameToLayer("PauseMenu");
        game_paused = true;
    }

    public void unpause() {
        GameObject.FindGameObjectWithTag("Light").GetComponent<Light>().intensity = UNPAUSE_LIGHT_INTENSITY;
        main_camera.cullingMask &= ~(1 << LayerMask.NameToLayer("PauseMenu"));
        game_paused = false;
    }

    void OnGUI() {
        if (camera_state == CameraState.START_LEVEL) {
            Color c = Color.white;
            c.a = 1;
            GUI.color = c;
            GUI.skin = start_level_gui_skin;
            GUI.skin.box.fontSize = Mathf.RoundToInt(Screen.width * 0.037f);
            GUI.skin.box.padding = new RectOffset(GUI.skin.box.fontSize, GUI.skin.box.fontSize,
                    Mathf.RoundToInt(GUI.skin.box.fontSize * 0.75f), Mathf.RoundToInt(GUI.skin.box.fontSize * 0.75f));
            GUI.Box(new Rect(Screen.width * 0.125f, Screen.height * 0.25f, Screen.width * 0.75f, Screen.height * 0.5f), getStartLevelText());
            GUI.color = Color.white;
        }
    }

    string getStartLevelText() {
        if (current_start_level_text.Length != START_LEVEL_TEXT[current_level].Length) {
            current_start_level_text += START_LEVEL_TEXT[current_level][current_start_level_text.Length];
        }
        return current_start_level_text;
    }
}
