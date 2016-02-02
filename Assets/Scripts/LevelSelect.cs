using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelSelect : MonoBehaviour {
    string[] LEVEL_SCENE_NAMES = {
        "_Level_1",
        "_Level_2F",
        "_Level_3",
        "_Level_Custom_NM",
        "_Level_Custom_AJ",
    };

    const KeyCode UP_KEY = KeyCode.UpArrow;
    const KeyCode DOWN_KEY = KeyCode.DownArrow;
    const KeyCode START_KEY = KeyCode.Return;
    const KeyCode CONFIRM_KEY = KeyCode.S;

    public GameObject header_text_object;
    public GameObject[] level_text_objects;
    GUIText header_text;
    GUIText[] level_texts;

    public float header_offset;
    public float level_list_offset_from_header;
    public float between_level_offset;
    public float header_text_size;
    public float level_text_size;

    public Color selected_color;
    public Color unselected_color;

    int selected_level;

	// Use this for initialization
	void Start () {
        header_text = header_text_object.GetComponent<GUIText>();
        level_texts = new GUIText[level_text_objects.Length];
        for (int i = 0; i < level_text_objects.Length; ++i) {
            level_texts[i] = level_text_objects[i].GetComponent<GUIText>();
        }

        header_offset = 0.85f;
        level_list_offset_from_header = 0.13f;
        between_level_offset = 0.11f;
        header_text_size = 0.07f;
        level_text_size = 0.055f;
        header_text.pixelOffset = new Vector2(Screen.width * 0.5f, Screen.height * header_offset);
        header_text.fontSize = Mathf.RoundToInt(Screen.width * header_text_size);

        float cur_level_offset = header_offset - level_list_offset_from_header;
        for (int i = 0; i < level_text_objects.Length; ++i) {
            level_texts[i].pixelOffset = new Vector2(Screen.width * 0.5f, Screen.height * cur_level_offset);
            level_texts[i].fontSize = Mathf.RoundToInt(Screen.width * level_text_size);
            level_texts[i].color = unselected_color;
            cur_level_offset -= between_level_offset;
        }

        selected_level = 0;
        level_texts[0].color = selected_color;
        SelectorBox.box.setStartPosition(getSelectedLevelPosition());
	}

    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(START_KEY) || Input.GetKeyDown(CONFIRM_KEY)) {
            SceneManager.LoadScene(LEVEL_SCENE_NAMES[selected_level]);
        }

        level_texts[selected_level].color = unselected_color;
        if (Input.GetKeyDown(UP_KEY)) {
            --selected_level;
            if (selected_level == -1) {
                selected_level = level_texts.Length - 1;
            }
            SelectorBox.box.moveSelectorBox(getSelectedLevelPosition());
        } else if (Input.GetKeyDown(DOWN_KEY)) {
            ++selected_level;
            if (selected_level == level_texts.Length) {
                selected_level = 0;
            }
            SelectorBox.box.moveSelectorBox(getSelectedLevelPosition());
        }
        level_texts[selected_level].color = selected_color;
    }

    Vector3 getSelectedLevelPosition() {
        float level_offset = header_offset - level_list_offset_from_header -
            (between_level_offset * selected_level);
        return new Vector3(0f, 0f, (level_offset - 0.5f) * 10f);
    }
}
