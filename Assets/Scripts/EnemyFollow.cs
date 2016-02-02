using UnityEngine;
using System.Collections;

public class EnemyFollow : MonoBehaviour {
    public GameObject enemy;

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        if (enemy.GetComponent<Enemy>().dead)
            Destroy(this.gameObject);
        gameObject.transform.position = enemy.transform.position;
    }
}
