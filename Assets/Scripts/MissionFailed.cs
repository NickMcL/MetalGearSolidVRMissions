using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MissionFailed : MonoBehaviour {
    const KeyCode UP_KEY = KeyCode.UpArrow;
    const KeyCode DOWN_KEY = KeyCode.DownArrow;
    const KeyCode START_KEY = KeyCode.Return;
    const KeyCode CONFIRM_KEY = KeyCode.S;

    public GameObject header_text_object;
    public GameObject[] option_text_objects;
    GUIText header_text;
    GUIText[] option_texts;

    public float header_offset;
    public float level_list_offset_from_header;
    public float between_level_offset;
    public float header_text_size;
    public float option_text_size;

    public Color selected_color;
    public Color unselected_color;

    public static string current_level_scene_name;
    public string start_menu_scene_name;

    int selected_option;

	// Use this for initialization
	void Start () {
        header_text = header_text_object.GetComponent<GUIText>();
        option_texts = new GUIText[option_text_objects.Length];
        for (int i = 0; i < option_text_objects.Length; ++i) {
            option_texts[i] = option_text_objects[i].GetComponent<GUIText>();
        }

        header_offset = 0.85f;
        level_list_offset_from_header = 0.13f;
        between_level_offset = 0.11f;
        header_text_size = 0.07f;
        option_text_size = 0.055f;
        header_text.pixelOffset = new Vector2(Screen.width * 0.5f, Screen.height * header_offset);
        header_text.fontSize = Mathf.RoundToInt(Screen.width * header_text_size);

        float cur_level_offset = header_offset - level_list_offset_from_header;
        for (int i = 0; i < option_text_objects.Length; ++i) {
            option_texts[i].pixelOffset = new Vector2(Screen.width * 0.5f, Screen.height * cur_level_offset);
            option_texts[i].fontSize = Mathf.RoundToInt(Screen.width * option_text_size);
            option_texts[i].color = unselected_color;
            cur_level_offset -= between_level_offset;
        }

        selected_option = 0;
        option_texts[0].color = selected_color;
        SelectorBox.box.setStartPosition(getSelectedLevelPosition());
	}

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(START_KEY) || Input.GetKeyDown(CONFIRM_KEY)) {
            if (selected_option == 0) {
                SceneManager.LoadScene(current_level_scene_name);
            } else if (selected_option == 1) {
                SceneManager.LoadScene(start_menu_scene_name);
            }
        }

        option_texts[selected_option].color = unselected_color;
        if (Input.GetKeyDown(UP_KEY)) {
            --selected_option;
            if (selected_option == -1) {
                selected_option = option_texts.Length - 1;
            }
            SelectorBox.box.moveSelectorBox(getSelectedLevelPosition());
        } else if (Input.GetKeyDown(DOWN_KEY)) {
            ++selected_option;
            if (selected_option == option_texts.Length) {
                selected_option = 0;
            }
            SelectorBox.box.moveSelectorBox(getSelectedLevelPosition());
        }
        option_texts[selected_option].color = selected_color;
    }

    Vector3 getSelectedLevelPosition() {
        float level_offset = header_offset - level_list_offset_from_header -
            (between_level_offset * selected_option);
        return new Vector3(0f, 0f, (level_offset - 0.5f) * 10f);
    }
}

