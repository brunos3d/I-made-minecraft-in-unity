using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockDef {
    public string name;
    public bool useGravity = false;
    public bool isTranslucent = false;
    public bool isTransparent = false;
    public int materialIndex = 0;
    public float timeToDestroyParticles = 3.0f;
    public List<string> tags = new List<string>();
    public List<int> tiles = new List<int>();
}

public class World : MonoBehaviour {

    public static World currentWorld { get; private set; }
    public static int airDef { get; private set; }
    public static int dirtWithGrassDef { get; private set; }
    public static int dirtDef { get; private set; }
    public static int bedrockDef { get; private set; }
    public static int diamondDef { get; private set; }
    public static int stoneDef { get; private set; }
    public static int waterDef { get; private set; }
    public static int sandDef { get; private set; }

    [Header("Use Randomize as Button")]
    public bool randomize = false;
    public GameObject chunkPrefab;
    public GameObject cloudsChunkPrefab;
    public GameObject particlesPrefab;
    public GameObject gravityBlockPrefab;
    public int initialFOVMultiplier = 2;
    public Vector3Int playerFOV = new Vector3Int(32, 32, 32);
    public Vector3Int chunkSize = new Vector3Int(16, 16, 16);
    public int cloudsHeight = 64;
    public float waterLevel = 40.0f;
    [Range(0.0f, 1.0f)]
    public float cloudsCoverage = 0.7f;
    public bool usePositionAsOffset = true;
    public bool randomNoise = true;
    public int fractalLevel = 5;
    public float noiseScale = 10.0f;
    public Vector3 noiseOffset;
    public float chunckUpdateTime = 1.0f;
    public int grassGrowProb = 10;
    public int grassDeathProb = 10;
    public Material cloudsMaterial;
    public List<Material> materials = new List<Material>();
    public List<BlockDef> blockDefs = new List<BlockDef>();
    public Dictionary<string, int> defIndexes = new Dictionary<string, int>();

    float t_chunckUpdateTime = 0.0f;
    Transform player;
    List<Chunk> chunkMap = new List<Chunk>();
    List<CloudsChunk> cloudsChunkMap = new List<CloudsChunk>();

    void Awake() {
        World.currentWorld = this;

        for (int idx = 0; idx < blockDefs.Count; idx++) {
            defIndexes[blockDefs[idx].name] = idx;
        }

        World.airDef = this.defIndexes["Air"];
        World.dirtWithGrassDef = this.defIndexes["DirtWithGrass"];
        World.dirtDef = this.defIndexes["Dirt"];
        World.bedrockDef = this.defIndexes["Bedrock"];
        World.diamondDef = this.defIndexes["Diamond"];
        World.stoneDef = this.defIndexes["Stone"];
        World.waterDef = this.defIndexes["Water"];
        World.sandDef = this.defIndexes["Sand"];
    }

    void Start() {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        Vector3 ppos = player.position;

        if (randomNoise) {
            noiseScale = Random.Range(32, 128);
            noiseOffset = Random.insideUnitSphere * noiseScale * noiseScale;
        }

        for (float x = ppos.x - playerFOV.x * initialFOVMultiplier; x < ppos.x + playerFOV.x * initialFOVMultiplier; x += chunkSize.x) {
            for (float z = ppos.z - playerFOV.y * initialFOVMultiplier; z < ppos.z + playerFOV.y * initialFOVMultiplier; z += chunkSize.z) {
                int xGridCoord = Mathf.FloorToInt(x / chunkSize.x) * chunkSize.x;
                int zGridCoord = Mathf.FloorToInt(z / chunkSize.z) * chunkSize.z;

                var terrainInstance = Instantiate(chunkPrefab, new Vector3(xGridCoord, 0, zGridCoord), Quaternion.identity);
                var cloudInstance = Instantiate(cloudsChunkPrefab, new Vector3(xGridCoord, cloudsHeight, zGridCoord), Quaternion.identity);

                terrainInstance.transform.SetParent(transform);
                cloudInstance.transform.SetParent(transform);

                chunkMap.Add(terrainInstance.GetComponent<Chunk>());
                cloudsChunkMap.Add(cloudInstance.GetComponent<CloudsChunk>());
            }
        }
    }

    void Update() {
        if (randomize) {
            noiseScale = Random.Range(32, 128);
            noiseOffset = Random.insideUnitSphere * noiseScale * noiseScale;
            for (int idx = 0; idx < chunkMap.Count; idx++) {
                Chunk chunk = chunkMap[idx];
                StartCoroutine(chunk.GenerateChunk());
            }
            randomize = false;
            return;
        }

        Vector3 ppos = player.position;
        t_chunckUpdateTime += Time.deltaTime;

        for (int idx = 0; idx < chunkMap.Count; idx++) {
            Chunk chunk = chunkMap[idx];
            CloudsChunk cloudsChunk = cloudsChunkMap[idx];

            Vector2 biPlayerPos = new Vector2(player.position.x, player.position.z);
            Vector2 biChunkPos = new Vector2(chunk.transform.position.x, chunk.transform.position.z);

            bool activeChunk = Vector3.Distance(biPlayerPos, biChunkPos) <= playerFOV.magnitude;

            chunk.gameObject.SetActive(activeChunk);

            if (activeChunk) {
                chunk.gameObject.SetActive(true);
            } else {
                if (chunk.CoroutinesRunning == 0) {
                    chunk.gameObject.SetActive(false);
                }
            }

            // utopia
            if (activeChunk && t_chunckUpdateTime >= chunckUpdateTime && chunk.CoroutinesRunning == 0) {
                StartCoroutine(chunk.RefreshMap());
            }
        }

        if (t_chunckUpdateTime >= chunckUpdateTime) {
            Chunk chunk = TryGetChunk(ppos);
            if (chunk != null) {
                StartCoroutine(chunk.RefreshMap());
            }
            t_chunckUpdateTime = 0.0f;
        }

        for (float x = ppos.x - playerFOV.x; x < ppos.x + playerFOV.x; x += chunkSize.x) {
            for (float z = ppos.z - playerFOV.y; z < ppos.z + playerFOV.y; z += chunkSize.z) {
                Vector3 idxpos = new Vector3(x, 0, z);

                Chunk chunk = TryGetChunk(idxpos);
                CloudsChunk cloudsChunk = TryGetCloudsChunk(idxpos);

                if (chunk == null) {
                    int xGridCoord = Mathf.FloorToInt(x / chunkSize.x) * chunkSize.x;
                    int zGridCoord = Mathf.FloorToInt(z / chunkSize.z) * chunkSize.z;

                    var instance = Instantiate(chunkPrefab, new Vector3(xGridCoord, 0, zGridCoord), Quaternion.identity).GetComponent<Chunk>();

                    instance.transform.SetParent(transform);
                    chunkMap.Add(instance);
                }

                if (cloudsChunk == null) {
                    int xGridCoord = Mathf.FloorToInt(x / chunkSize.x) * chunkSize.x;
                    int zGridCoord = Mathf.FloorToInt(z / chunkSize.z) * chunkSize.z;

                    var instance = Instantiate(cloudsChunkPrefab, new Vector3(xGridCoord, cloudsHeight, zGridCoord), Quaternion.identity).GetComponent<CloudsChunk>();

                    instance.transform.SetParent(transform);
                    cloudsChunkMap.Add(instance);
                }
            }
        }
    }

    // void OnDrawGizmos() {
    //     if (player) {
    //         Chunk chunk = TryGetChunk(player.position);
    //         if (chunk != null) {
    //             chunk.DrawGizmos(Color.white);
    //         }
    //     }
    // }

    public void InsertGravityBlockAt(int x, int y, int z, int block) {
        Vector3 mid = new Vector3(0.5f, 0.5f, 0.5f);

        var instance = Instantiate(gravityBlockPrefab, new Vector3(x, y, z) + mid, Quaternion.identity);
        var gravityBlock = instance.GetComponent<GravityBlock>();

        gravityBlock.Setup(block);
    }

    public void InsertBlockParticlesAt(Vector3 point, int block) {
        InsertBlockParticlesAt(Vector3Int.FloorToInt(point), block);
    }
    public void InsertBlockParticlesAt(Vector3Int point, int block) {
        InsertBlockParticlesAt(point.x, point.y, point.z, block);
    }

    public void InsertBlockParticlesAt(int x, int y, int z, int block) {
        Vector3 mid = new Vector3(0.5f, 0.5f, 0.5f);

        var instance = Instantiate(particlesPrefab, new Vector3(x, y, z) + mid, Quaternion.identity);
        var blockParticles = instance.GetComponent<BlockParticles>();

        blockParticles.Setup(block, blockDefs[block].timeToDestroyParticles);
    }

    public int FindBlockDefByName(string name) {
        int result = -1;
        if (defIndexes.TryGetValue(name, out result)) {
            return result;
        }
        return result;
    }

    public bool ChunkExists(Vector3 position) {
        return ChunkExists(Vector3Int.FloorToInt(position));
    }

    public bool ChunkExists(Vector3Int position) {
        return ChunkExists(position.x, position.y, position.z);
    }
    public bool ChunkExists(int x, int y, int z) {
        for (int idx = 0; idx < chunkMap.Count; idx++) {
            Chunk chunk = chunkMap[idx];
            if (x < chunk.transform.position.x || z < chunk.transform.position.z || x >= chunk.transform.position.x + chunkSize.x || z >= chunk.transform.position.z + chunkSize.z) continue;
            return true;
        }
        return false;
    }

    public Chunk TryGetChunk(Vector3 position) {
        return TryGetChunk(Vector3Int.FloorToInt(position));
    }

    public Chunk TryGetChunk(Vector3Int position) {
        return TryGetChunk(position.x, position.y, position.z);
    }
    public Chunk TryGetChunk(int x, int y, int z) {
        for (int idx = 0; idx < chunkMap.Count; idx++) {
            Chunk chunk = chunkMap[idx];
            if (x < chunk.transform.position.x || z < chunk.transform.position.z || x >= chunk.transform.position.x + chunkSize.x || z >= chunk.transform.position.z + chunkSize.z) continue;
            return chunk;
        }
        return null;
    }

    public CloudsChunk TryGetCloudsChunk(Vector3 position) {
        return TryGetCloudsChunk(Vector3Int.FloorToInt(position));
    }

    public CloudsChunk TryGetCloudsChunk(Vector3Int position) {
        return TryGetCloudsChunk(position.x, position.y, position.z);
    }
    public CloudsChunk TryGetCloudsChunk(int x, int y, int z) {
        for (int idx = 0; idx < cloudsChunkMap.Count; idx++) {
            CloudsChunk chunk = cloudsChunkMap[idx];
            if (x < chunk.transform.position.x || z < chunk.transform.position.z || x >= chunk.transform.position.x + chunkSize.x || z >= chunk.transform.position.z + chunkSize.z) continue;
            return chunk;
        }
        return null;
    }
}