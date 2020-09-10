using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LimboTeleport : MonoBehaviour {

    public int limboStart = -32;
    public int bPointOffset = 0;

    World t_World;

    void Start() {
        t_World = GameObject.FindObjectOfType<World>();
    }

    void LateUpdate() {
        if (t_World != null && transform.position.y <= limboStart) {
            transform.position = new Vector3(transform.position.x + Random.Range(-16, 16), t_World.chunkSize.y + bPointOffset, transform.position.z + Random.Range(-16, 16));
        }
    }
}