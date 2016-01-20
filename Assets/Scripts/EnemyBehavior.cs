using UnityEngine;
using System.Collections;

public class EnemyBehavior : MonoBehaviour {
    public float speed = 10f;

    public GameObject next_point;
    Rigidbody body;
    NavMeshAgent mesh_agent;
    Color default_color;
    RaycastHit seeSnake;
    float detect_range = 5; //Max detection range
	// Use this for initialization
	void Start () {
        body = gameObject.GetComponent<Rigidbody>();
        mesh_agent = gameObject.GetComponent<NavMeshAgent>();
        default_color = this.GetComponent<Renderer>().material.color;
	}
	
	// Update is called once per frame
	void Update () {
        /*
        Vector3 dis = next_point.transform.position - gameObject.transform.position;
        body.velocity = dis.normalized * speed;
        body.transform.forward = dis.normalized;
        */
        mesh_agent.destination = next_point.transform.position;
        Vector3 to_player = MovementController.player.transform.position - gameObject.transform.position;
        
        Physics.Raycast(this.transform.position, to_player, out seeSnake);
        if (Vector3.Angle(this.transform.forward, to_player) < 30f && seeSnake.collider.gameObject.tag == "Player" && seeSnake.distance<detect_range)
        {
            Debug.DrawRay(this.transform.position, to_player);
            Invoke("detectPlayer", 0.5f);//0.5 is detection delay
        }
	}
    //Confirmation of proper detection
    void detectPlayer()
    {
        Vector3 to_player = MovementController.player.transform.position - gameObject.transform.position;
        Color surprised=Color.yellow;
        Physics.Raycast(this.transform.position, to_player, out seeSnake);
        if (Vector3.Angle(this.transform.forward, to_player) < 30f && seeSnake.collider.gameObject.tag == "Player" && seeSnake.distance < detect_range)
        {
            this.GetComponent<Renderer>().material.color = surprised;
            Invoke("undetectPlayer", 1);
        }

    }
    //for when player leaves sight.
    void undetectPlayer()
    {
        this.GetComponent<Renderer>().material.color = default_color;
    }
    public void setNext(GameObject new_next_point) {
        next_point = new_next_point;
    }
}
