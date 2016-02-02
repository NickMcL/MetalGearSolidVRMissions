using UnityEngine;
using System.Collections;

public class AudioController : MonoBehaviour {
    public static AudioController audioPlayer;
    AudioSource audio;
   public AudioClip punch;
    public AudioClip step;
    public AudioClip knock;
    public AudioClip spotted;
    public AudioClip GG;
    public AudioClip grab;
    public AudioClip choke;
    public AudioClip whiff;
    public AudioClip crouch;
    public AudioClip crawl;
    public AudioClip cant;
    public AudioClip what;
    public AudioClip hit_ground;
    float punch_delay = 0;
    float step_delay = 0;
    float cant_delay = 0;
    float volume = 10;
	// Use this for initialization
	void Start () {
        audioPlayer = this;
        audio = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
        if (step_delay > 0)
            step_delay -= Time.deltaTime;
        if (punch_delay > 0)
            punch_delay -= Time.deltaTime;
        if (cant_delay > 0)
            cant_delay -= Time.deltaTime;
	}
    public void stepSound() {
        if (step_delay > 0)
            return;
        step_delay = 0.4f;
        audio.PlayOneShot(step,volume);
    }
   public void punchSound() {
        if (punch_delay > 0)
            return;
        punch_delay = 0.3f;
        audio.PlayOneShot(punch,volume);
    }
   public void knockSound() {
        if (punch_delay > 0)
            return;
        punch_delay = 0.3f;
        audio.PlayOneShot(knock,volume);
    }
   public void grabSound() {
       audio.PlayOneShot(grab, volume);
   }
   public void chokeSound() {
       audio.PlayOneShot(choke, volume);
   }
   public void whiffSound() {
       audio.PlayOneShot(whiff, volume);
   }
   public void cantSound() {
       if (cant_delay > 0)
           return;
       cant_delay = 3;
       audio.PlayOneShot(cant, volume);

   }
   public void whatSound() {
       audio.PlayOneShot(what, volume);
       
   }
   public void hitGround() {
       audio.PlayOneShot(hit_ground, volume);
   }
   public void spotSound() {
       audio.Stop();
       audio.PlayOneShot(spotted,volume);
       Invoke("gameOver", 0.3f);
   }
   void gameOver() {
        audio.PlayOneShot(GG);
   }
}
