using UnityEngine;

public class GravityBlock : MonoBehaviour {

    int block = -1;

    Material t_material;
    Rigidbody t_Rigidbody;
    MeshRenderer t_Renderer;

    public void Setup(int block) {
        t_Rigidbody = GetComponent<Rigidbody>();
        t_Renderer = GetComponent<MeshRenderer>();

        this.block = block;

        if (World.currentWorld != null) {
            var blockDef = World.currentWorld.blockDefs[block];
            int texIndex = blockDef.tiles[0];

            Material material = World.currentWorld.materials[blockDef.materialIndex];

            t_material = new Material(material);

            t_material.mainTextureScale = Vector2.one * 0.25f;
            t_material.mainTextureOffset = new Vector2((texIndex % 4) * 0.25f, (texIndex / 4) * 0.25f);

            t_Renderer.material = t_material;
        }
    }

    void Update() {
        if (t_Rigidbody.velocity.magnitude == 0) {
            PlaceBlock(transform.position);
        }
    }

    void OnCollisionEnter(Collision other) {
        if (other.gameObject.CompareTag("Chunk")) {
            PlaceBlock(other.contacts[0].point);
        }
    }

    public void PlaceBlock(Vector3 position) {
        Chunk chunk = World.currentWorld.TryGetChunk(transform.position);

        if (chunk != null) {
            Vector3Int placePoint = Vector3Int.FloorToInt(transform.position - chunk.transform.position + new Vector3(0.0f, 0.5f, 0.0f));

            chunk.InsertBlockAt(placePoint, block);
            // Debug.Log($"(BG) Adicionado bloco {World.currentWorld.blockDefs[block].name} na posicao {placePoint}");

            Destroy(gameObject);
            Destroy(t_material);
        }
    }
}