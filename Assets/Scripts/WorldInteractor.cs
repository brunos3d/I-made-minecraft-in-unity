using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldInteractor : MonoBehaviour {

    public int placeBlock = 1;
    public float minNewBlockDistance = 2.7f;
    public float minExistingBlockDistance = 2.7f;
    public float timeToInteract = 0.3f;

    [Header("Debug Info")]
    public int d_targetBlock = 0;
    public Vector3 d_chunkPosition;
    public Vector3 d_worldHitPoint;
    public Vector3 d_worldBlockPoint;
    public Vector3 d_chunkHitPoint;
    public Vector3 d_chunkBlockPoint;
    public float d_targetNewBlockDistance;
    public float d_targetExistingBlockDistance;
    public float d_timeToInteractDelta;

    float t_timeToInteractDelta = 0.0f;

    void Update() {
        if (Input.mouseScrollDelta.y > 0) {
            placeBlock++;
        }
        if (Input.mouseScrollDelta.y < 0) {
            placeBlock--;
        }
        placeBlock = Mathf.Max(placeBlock, 1);
        placeBlock = Mathf.Min(placeBlock, World.currentWorld.blockDefs.Count - 1);

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) {
            // increase time
            t_timeToInteractDelta += Time.deltaTime;

        } else {
            // reset time
            t_timeToInteractDelta = 0.0f;
        }

        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) {
            t_timeToInteractDelta = timeToInteract;
        }

        d_timeToInteractDelta = t_timeToInteractDelta;

        RaycastHit hit;
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        int cookerDef = World.currentWorld.FindBlockDefByName("Cooker");
        int cookerActiveDef = World.currentWorld.FindBlockDefByName("CookerActive");
        // int craftTableDef = World.currentWorld.FindBlockDefByName("CraftTable");

        int targetBlock = -1;

        d_targetBlock = targetBlock;
        d_targetNewBlockDistance = -1.0f;
        d_targetExistingBlockDistance = -1.0f;

        if (Physics.Raycast(ray, out hit)) {
            if (hit.transform && hit.transform.GetComponent<Chunk>()) {
                Vector3 mid = new Vector3(0.5f, 0.5f, 0.5f);

                Chunk chunk = hit.transform.GetComponent<Chunk>();

                Vector3 chunkPosition = chunk.transform.position;
                d_chunkPosition = chunkPosition;

                Vector3 worldHitPoint = hit.point + hit.normal / 2;
                worldHitPoint = Vector3Int.FloorToInt(worldHitPoint);

                d_worldHitPoint = worldHitPoint;

                Vector3 worldBlockPoint = hit.point - hit.normal / 2;
                worldBlockPoint = Vector3Int.FloorToInt(worldBlockPoint);

                d_worldBlockPoint = worldBlockPoint;

                Vector3 chunkHitPoint = worldHitPoint - chunkPosition;
                Vector3 chunkBlockPoint = worldBlockPoint - chunkPosition;

                d_chunkHitPoint = chunkHitPoint;
                d_chunkBlockPoint = chunkBlockPoint;

                float targetNewBlockDistance = Vector3.Distance(transform.position, worldHitPoint + mid);
                float targetExistingBlockDistance = Vector3.Distance(transform.position, worldBlockPoint + mid);

                d_targetNewBlockDistance = targetNewBlockDistance;
                d_targetExistingBlockDistance = targetExistingBlockDistance;

                targetBlock = chunk.GetBlockAt(chunkBlockPoint);
                d_targetBlock = targetBlock;

                if (t_timeToInteractDelta >= timeToInteract) {
                    // reset time
                    t_timeToInteractDelta = 0.0f;
                    d_timeToInteractDelta = t_timeToInteractDelta;

                    if (Input.GetMouseButton(0)) {
                        chunk.RemoveBlockAt(chunkBlockPoint);
                        World.currentWorld.InsertBlockParticlesAt(worldBlockPoint, targetBlock);
                        // Debug.Log($"Removido bloco {World.currentWorld.blockDefs[targetBlock].name} na posicao {chunkBlockPoint}");
                    } else if (Input.GetMouseButton(1)) {
                        if (targetBlock == cookerDef) {
                            chunk.SetBlockAt(chunkBlockPoint, cookerActiveDef);
                            // Debug.Log($"Bloco {World.currentWorld.blockDefs[targetBlock].name} alterado para {World.currentWorld.blockDefs[cookerActiveDef].name} na posicao {chunkBlockPoint}");
                            return;
                        }
                        if (targetBlock == cookerActiveDef) {
                            chunk.SetBlockAt(chunkBlockPoint, cookerDef);
                            // Debug.Log($"Bloco {World.currentWorld.blockDefs[targetBlock].name} alterado para {World.currentWorld.blockDefs[cookerDef].name} na posicao {chunkBlockPoint}");
                            return;
                        }
                        if (targetNewBlockDistance >= minNewBlockDistance && targetExistingBlockDistance >= minExistingBlockDistance) {
                            chunk.InsertBlockAt(chunkHitPoint, placeBlock);
                            // Debug.Log($"Adicionado bloco {World.currentWorld.blockDefs[placeBlock].name} na posicao {chunkHitPoint}");
                            return;
                        }
                    }
                }
            }
        }
    }

    // void OnDrawGizmos() {
    //     if (d_targetBlock != -1) {
    //         Gizmos.color = Color.black;
    //         Vector3 mid = new Vector3(0.5f, 0.5f, -0.5f);
    //         Gizmos.DrawWireCube(d_worldBlockPoint + mid, Vector3.one);
    //     }
    // }
}