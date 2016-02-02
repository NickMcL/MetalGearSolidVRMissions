using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class StartMenu : MonoBehaviour {
    public string level_select_scene_name;
    const KeyCode START_KEY = KeyCode.Return;

    public Texture logo_image;
    public GameObject start_text_object;
    public float logo_pos = 0.17f;
    public float logo_size = 0.67f;
    public float text_y = 0.25f;
    public float text_x = 0.5f;
    public float text_font = 0.055f;
    GUIText start_text;
    Color gui_color;

    public float fade_logo_duration = 2.0f;
    float fade_logo_start_time;
    bool fading_logo;

    public float fade_start_text_duration = 1.0f;
    float fade_start_text_start_time;
    float fade_start_text_start_alpha;
    float fade_start_text_end_alpha;
    bool fade_start_text;

    // Use this for initialization
    void Start() {
        start_text = this.gameObject.GetComponent<GUIText>();

        gui_color = Color.white;
        fade_start_text = false;
        fadeInLogo();
    }

    // Update is called once per frame
    void Update() {
        float logo_alpha = 1;
        if (fading_logo) {
            if (Input.anyKey) {
                logo_alpha = 1;
            } else {
                logo_alpha = Mathf.Lerp(0f, 1f, (Time.time - fade_logo_start_time) / fade_logo_duration);
            }
            gui_color.a = logo_alpha;
            if (logo_alpha == 1) {
                fading_logo = false;
                fadeStartText();
            }
        }

        if (fade_start_text) {
            if (Input.GetKey(START_KEY)) {
                AudioController.audioPlayer.audio.Stop();
                SceneManager.LoadScene(level_select_scene_name);
            }

            Color text_color = start_text.color;
            text_color.a = Mathf.Lerp(fade_start_text_start_alpha, fade_start_text_end_alpha,
                (Time.time - fade_start_text_start_time) / fade_start_text_duration);
            if (text_color.a == fade_start_text_end_alpha) {
                float temp = fade_start_text_start_alpha;
                fade_start_text_start_alpha = fade_start_text_end_alpha;
                fade_start_text_end_alpha = temp;
                fade_start_text_start_time = Time.time;
            }
            start_text.color = text_color;
            start_text.pixelOffset = new Vector2(Screen.width * text_x, Screen.height * text_y);
            start_text.fontSize = Mathf.CeilToInt(Screen.width * text_font);
        }
    }

    void fadeInLogo() {
        fade_logo_start_time = Time.time;
        fading_logo = true;
    }

    void fadeStartText() {
        fade_start_text = true;
        fade_start_text_start_time = Time.time;
        fade_start_text_start_alpha = 0.4f;
        fade_start_text_end_alpha = 1;
    }

    void OnGUI() {
        GUI.color = gui_color;
        GUI.DrawTexture(new Rect(Screen.width * logo_pos, 0, Screen.height * logo_size * 1.5f, (Screen.height * logo_size)),
            logo_image, ScaleMode.ScaleToFit);
        GUI.color = Color.white;
    }
}
