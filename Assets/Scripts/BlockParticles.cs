using UnityEngine;

public class BlockParticles : MonoBehaviour {

    int block = -1;

    Material t_material;
    Rigidbody t_Rigidbody;
    ParticleSystemRenderer t_Renderer;

    public void Setup(int block, float timeToAutoDestroy = 1.0f) {
        t_Renderer = GetComponent<ParticleSystemRenderer>();

        this.block = block;

        if (World.currentWorld != null) {
            var blockDef = World.currentWorld.blockDefs[block];
            int texIndex = blockDef.tiles[2];

            Material material = World.currentWorld.materials[blockDef.materialIndex];

            t_material = new Material(material);

            t_material.mainTextureScale = Vector2.one * 0.1f;
            t_material.mainTextureOffset = new Vector2((texIndex % 4) * 0.25f, (texIndex / 4) * 0.25f);

            t_Renderer.material = t_material;
        }
        Destroy(gameObject, timeToAutoDestroy);
    }
}