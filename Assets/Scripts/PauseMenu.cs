using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class PauseMenu : MonoBehaviour {
    enum PauseOption {
        CONTINUE,
        RESTART,
        EXIT
    };
    Dictionary<int, PauseOption> MENU_OPTION_MAPPING = new Dictionary<int, PauseOption>() {
        {0, PauseOption.CONTINUE },
        {1, PauseOption.RESTART },
        {2, PauseOption.EXIT },
    };

    const KeyCode UP_KEY = KeyCode.UpArrow;
    const KeyCode DOWN_KEY = KeyCode.DownArrow;
    const KeyCode START_KEY = KeyCode.Return;
    const KeyCode CONFIRM_KEY = KeyCode.S;
    const KeyCode BACK_KEY = KeyCode.A;

    public GameObject header_text_object;
    public GameObject[] menu_option_objects;
    GUIText header_text;
    GUIText[] menu_options;

    public string current_scene_name;
    public string start_menu_scene_name;

    public float header_offset;
    public float level_list_offset_from_header;
    public float between_level_offset;
    public float header_text_size;
    public float menu_option_size;
    public float pause_menu_length;

    public Camera main_camera;

    public Color selected_color;
    public Color unselected_color;

    public bool game_paused;
    int selected_option;
    float control_delay_duraiton = 0.2f;
    float control_delay_start;

	// Use this for initialization
	void Start () {
        header_text = header_text_object.GetComponent<GUIText>();
        menu_options = new GUIText[menu_option_objects.Length];
        for (int i = 0; i < menu_option_objects.Length; ++i) {
            menu_options[i] = menu_option_objects[i].GetComponent<GUIText>();
        }

        //header_offset = 0.85f;
        //level_list_offset_from_header = 0.13f;
        //between_level_offset = 0.11f;
        //header_text_size = 0.07f;
        //menu_option_size = 0.055f;

        selected_option = 0;
        menu_options[0].color = selected_color;
        game_paused = false;
    }

    // Update is called once per frame
    void Update() {
        setTextPositions();
        if (!game_paused || (Time.time - control_delay_start) < control_delay_duraiton) {
            return;
        }
        if (Input.GetKeyDown(BACK_KEY)) {
            game_paused = false;
            CameraController.cam_control.unpause();
            return;
        }
        if (Input.GetKeyDown(START_KEY) || Input.GetKeyDown(CONFIRM_KEY)) {
            doPauseMenuAction(MENU_OPTION_MAPPING[selected_option]);
            return;
        }

        menu_options[selected_option].color = unselected_color;
        if (Input.GetKeyDown(UP_KEY)) {
            --selected_option;
            if (selected_option == -1) {
                selected_option = menu_options.Length - 1;
            }
        } else if (Input.GetKeyDown(DOWN_KEY)) {
            ++selected_option;
            if (selected_option == menu_options.Length) {
                selected_option = 0;
            }
        }
        menu_options[selected_option].color = selected_color;
    }

    void doPauseMenuAction(PauseOption action) {
        if (action == PauseOption.CONTINUE) {
            game_paused = false;
            CameraController.cam_control.unpause();
        } else if (action == PauseOption.RESTART) {
            SceneManager.LoadScene(current_scene_name);
        } else if (action == PauseOption.EXIT) {
            SceneManager.LoadScene(start_menu_scene_name);
        }
    }

    public void openPauseMenu() {
        setTextPositions();
        selected_option = 0;
        menu_options[0].color = selected_color;
        game_paused = true;
        control_delay_start = Time.time;
    }

    void setTextPositions() {
        header_text.pixelOffset = Vector2.zero;
        header_text.transform.position = new Vector2(0.5f, header_offset);
        header_text.fontSize = Mathf.RoundToInt(Screen.width * header_text_size);

        float cur_level_offset = header_offset - level_list_offset_from_header;
        for (int i = 0; i < menu_option_objects.Length; ++i) {
            menu_options[i].pixelOffset = Vector2.zero;
            menu_options[i].transform.position = new Vector2(0.5f, cur_level_offset);
            menu_options[i].fontSize = Mathf.RoundToInt(Screen.width * menu_option_size);
            menu_options[i].color = unselected_color;
            cur_level_offset -= between_level_offset;
        }
    }

}
