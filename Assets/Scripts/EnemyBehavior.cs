using UnityEngine;
using System.Collections;

public class EnemyBehavior : MonoBehaviour {
    public float speed = 10f;

    public GameObject next_point;
    Rigidbody body;
    NavMeshAgent mesh_agent;

	// Use this for initialization
	void Start () {
        body = gameObject.GetComponent<Rigidbody>();
        mesh_agent = gameObject.GetComponent<NavMeshAgent>();
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
	}

    public void setNext(GameObject new_next_point) {
        next_point = new_next_point;
    }
}
