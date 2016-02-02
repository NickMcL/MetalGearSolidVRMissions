using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class LevelFinish : MonoBehaviour {
    Dictionary<string, string> LEVEL_SCENE_NAME_MAP = new Dictionary<string, string>() {
        { SceneNames.LEVEL_1, "Level 1"},
        { SceneNames.LEVEL_2, "Level 2"},
        { SceneNames.LEVEL_3, "Level 3"},
        { SceneNames.CUSTOM_NM, "Custom  Level 1" },
        { SceneNames.CUSTOM_AJ, "Custom  Level 2" },
    };

    const KeyCode UP_KEY = KeyCode.UpArrow;
    const KeyCode DOWN_KEY = KeyCode.DownArrow;
    const KeyCode START_KEY = KeyCode.Return;
    const KeyCode CONFIRM_KEY = KeyCode.S;

    public GameObject header_text_object;
    public GameObject[] option_text_objects;
    GUIText header_text;
    GUIText[] option_texts;

    public float header_offset;
    public float option_list_offset_from_header;
    public float between_option_offset;
    public float header_text_size;
    public float option_text_size;

    public static bool last_level = false;
    public static string next_level_scene_name;
    public static string current_level_scene_name = SceneNames.LEVEL_1;

    public Color selected_color;
    public Color unselected_color;

    int selected_option;
    bool sound_waiting;

	// Use this for initialization
	void Start () {
        if (last_level) {
            adjustMenuForLastLevel();
        }
        header_text = header_text_object.GetComponent<GUIText>();
        option_texts = new GUIText[option_text_objects.Length];
        for (int i = 0; i < option_text_objects.Length; ++i) {
            option_texts[i] = option_text_objects[i].GetComponent<GUIText>();
        }

        header_offset = 0.85f;
        option_list_offset_from_header = 0.13f;
        between_option_offset = 0.11f;
        header_text_size = 0.07f;
        option_text_size = 0.055f;
        header_text.pixelOffset = new Vector2(Screen.width * 0.5f, Screen.height * header_offset);
        header_text.fontSize = Mathf.RoundToInt(Screen.width * header_text_size);
        header_text.text = LEVEL_SCENE_NAME_MAP[current_level_scene_name] + "  Cleared";

        float cur_option_offset = header_offset - option_list_offset_from_header;
        for (int i = 0; i < option_text_objects.Length; ++i) {
            option_texts[i].pixelOffset = new Vector2(Screen.width * 0.5f, Screen.height * cur_option_offset);
            option_texts[i].fontSize = Mathf.RoundToInt(Screen.width * option_text_size);
            option_texts[i].color = unselected_color;
            cur_option_offset -= between_option_offset;
        }

        selected_option = 0;
        option_texts[0].color = selected_color;
        SelectorBox.box.setStartPosition(getSelectedLevelPosition());
        sound_waiting = false;
	}

    // Update is called once per frame
    void Update() {
        if (sound_waiting) {
            return;
        }
        if (Input.GetKeyDown(START_KEY) || Input.GetKeyDown(CONFIRM_KEY)) {
            sound_waiting = true;
            if (last_level) {
                if (selected_option == 0) {
                    AudioController.audioPlayer.gunshot();
                    Invoke("loadCurrentLevel", 2.5f);
                } else if (selected_option == 1) {
                    AudioController.audioPlayer.exitSound();
                    Invoke("loadStartMenu", 0.75f);
                }
            } else {
                if (selected_option == 0) {
                    AudioController.audioPlayer.gunshot();
                    Invoke("loadNextLevel", 2.5f);
                } else if (selected_option == 1) {
                    AudioController.audioPlayer.gunshot();
                    Invoke("loadCurrentLevel", 2.5f);
                } else if (selected_option == 2) {
                    AudioController.audioPlayer.gunshot();
                    Invoke("loadStartMenu", 0.75f);
                }
            }
        }

        option_texts[selected_option].color = unselected_color;
        if (Input.GetKeyDown(UP_KEY)) {
            --selected_option;
            if (selected_option == -1) {
                selected_option = option_texts.Length - 1;
            }
            SelectorBox.box.moveSelectorBox(getSelectedLevelPosition());
            AudioController.audioPlayer.menuSound();
        } else if (Input.GetKeyDown(DOWN_KEY)) {
            ++selected_option;
            if (selected_option == option_texts.Length) {
                selected_option = 0;
            }
            SelectorBox.box.moveSelectorBox(getSelectedLevelPosition());
            AudioController.audioPlayer.menuSound();
        }
        option_texts[selected_option].color = selected_color;
    }

    Vector3 getSelectedLevelPosition() {
        float option_offset = header_offset - option_list_offset_from_header -
            (between_option_offset * selected_option);
        return new Vector3(0f, 0f, (option_offset - 0.5f) * 10f);
    }

    void adjustMenuForLastLevel() {
        GameObject[] last_level_text_objects = new GameObject[option_text_objects.Length - 1];
        Destroy(option_text_objects[0]);
        for (int i = 1; i < option_text_objects.Length; ++i) {
            last_level_text_objects[i - 1] = option_text_objects[i];
        }
        option_text_objects = last_level_text_objects;
    }

    void loadNextLevel() {
        sound_waiting = false;
        SceneManager.LoadScene(next_level_scene_name);
    }

    void loadCurrentLevel() {
        sound_waiting = false;
        SceneManager.LoadScene(current_level_scene_name);
    }

    void loadStartMenu() {
        sound_waiting = false;
        SceneManager.LoadScene(SceneNames.START_MENU);
    }
}
