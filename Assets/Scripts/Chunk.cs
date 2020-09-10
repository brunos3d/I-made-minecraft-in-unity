using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

[System.Serializable]
public class BlockInfo {
    public int blockDef = -1; // no block
    public bool isLightSource = false;
    public Color color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
}

public class Chunk : MonoBehaviour {

    public int CoroutinesRunning = 0;

    [Header("Mesh Data")]
    List<Vector3> verts = new List<Vector3>();
    Dictionary<int, List<int>> tris = new Dictionary<int, List<int>> { { 0, new List<int>() } };
    List<Vector2> uv = new List<Vector2>();
    List<Color32> colors = new List<Color32>();
    List<Material> materials = new List<Material>();

    Color c_light = new Color(1.0f, 1.0f, 1.0f, 1.0f);
    Color c_dark = new Color(0.05f, 0.05f, 0.05f, 1.0f);
    int[, , ] map;

    Mesh t_mesh;
    MeshFilter t_meshFilter;
    MeshRenderer t_meshRenderer;
    MeshCollider t_meshCollider;
    Transform player;

    void Start() {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        t_meshRenderer = gameObject.AddComponent<MeshRenderer>();
        t_meshCollider = gameObject.AddComponent<MeshCollider>();

        t_meshFilter = gameObject.AddComponent<MeshFilter>();
        t_mesh = new Mesh();
        t_mesh.name = $"Chunk {t_mesh.GetInstanceID()}";
        t_mesh.MarkDynamic();

        t_meshFilter.mesh = t_mesh;
        t_meshCollider.sharedMesh = t_mesh;

        // GenerateChunk();
        StartCoroutine(GenerateChunk());
    }

    // void OnDrawGizmosSelected() {
    //     DrawGizmos(Color.red);
    // }

    public void DrawGizmos(Color color) {
        if (World.currentWorld != null) {
            Gizmos.color = color;
            Vector3 cubeCenterPos = transform.position + (World.currentWorld.chunkSize / 2) + Vector3.back;
            Gizmos.DrawWireCube(cubeCenterPos, World.currentWorld.chunkSize);
        }
    }
    public void InsertBlockAt(Vector3 point, int block, bool regenerate = true) {
        InsertBlockAt(Vector3Int.FloorToInt(point), block, regenerate);
    }

    public void InsertBlockAt(Vector3Int point, int block, bool regenerate = true) {
        InsertBlockAt(point.x, point.y, point.z, block, regenerate);
    }

    public void InsertBlockAt(int x, int y, int z, int block, bool regenerate = true) {
        if (IndexExists(x, y, z) && IsAirBlock(x, y, z)) {
            map[x, y, z] = block;
            if (regenerate) {
                // RegenerateChunk();
                StartCoroutine(RegenerateChunk());
            }
        }
    }

    public void RemoveBlockAt(Vector3 point, bool regenerate = true) {
        RemoveBlockAt(Vector3Int.FloorToInt(point), regenerate);
    }

    public void RemoveBlockAt(Vector3Int point, bool regenerate = true) {
        RemoveBlockAt(point.x, point.y, point.z, regenerate);
    }

    public void RemoveBlockAt(int x, int y, int z, bool regenerate = true) {
        if (IndexExists(x, y, z)) {
            map[x, y, z] = 0;
            if (regenerate) {
                // RegenerateChunk();
                StartCoroutine(RegenerateChunk());
            }
        }
    }

    public void SetBlockAt(Vector3 point, int block, bool regenerate = true) {
        SetBlockAt(Vector3Int.FloorToInt(point), block, regenerate);
    }

    public void SetBlockAt(Vector3Int point, int block, bool regenerate = true) {
        SetBlockAt(point.x, point.y, point.z, block, regenerate);
    }

    public void SetBlockAt(int x, int y, int z, int block, bool regenerate = true) {
        if (IndexExists(x, y, z)) {
            map[x, y, z] = block;
            if (regenerate) {
                // RegenerateChunk();
                StartCoroutine(RegenerateChunk());
            }
        }
    }

    public bool IsAirBlock(Vector3 point) {
        return IsAirBlock(Vector3Int.FloorToInt(point));
    }

    public bool IsAirBlock(Vector3Int point) {
        return IsAirBlock(point.x, point.y, point.z);
    }

    public bool IsAirBlock(int x, int y, int z) {
        return (!IndexExists(x, y, z)) || map[x, y, z] == 0;
    }

    public int GetBlockAt(Vector3 point) {
        return GetBlockAt(Vector3Int.FloorToInt(point));
    }

    public int GetBlockAt(Vector3Int point) {
        return GetBlockAt(point.x, point.y, point.z);
    }

    public int GetBlockAt(int x, int y, int z) {
        if (IndexExists(x, y, z)) {
            return map[x, y, z];
        } else {
            return -1;
        }
    }

    public bool IndexExists(Vector3 point) {
        return IndexExists(Vector3Int.FloorToInt(point));
    }

    public bool IndexExists(Vector3Int point) {
        return IndexExists(point.x, point.y, point.z);
    }

    public bool IndexExists(int x, int y, int z) {
        return !(x < 0 || x >= World.currentWorld.chunkSize.x || y < 0 || y >= World.currentWorld.chunkSize.y || z < 0 || z >= World.currentWorld.chunkSize.z);
    }

    public IEnumerator RegenerateChunk() {
        CoroutinesRunning++;
        yield return ResetChunk();
        yield return GenerateMeshData();
        yield return GenerateLightMap();
        yield return GenerateChunkMesh();
        CoroutinesRunning--;
        yield break;
    }

    public virtual IEnumerator GenerateChunk() {
        CoroutinesRunning++;
        yield return ResetChunk();
        yield return GenerateMap();
        yield return GenerateTrees();
        yield return GenerateLightMap();
        yield return GenerateMeshData();
        yield return GenerateChunkMesh();
        CoroutinesRunning--;
        yield break;
    }

    public IEnumerator ResetChunk() {
        // t_meshCollider.sharedMesh = null;
        // t_meshFilter.mesh = null;
        materials.Clear();
        verts.Clear();
        tris.Clear();
        uv.Clear();
        colors.Clear();
        yield break;
    }

    public IEnumerator RefreshMap() {
        CoroutinesRunning++;

        bool needsRegenerate = false;

        Dictionary<Vector3Int, int> gravityBlocksToSpawn = new Dictionary<Vector3Int, int>();

        for (int x = World.currentWorld.chunkSize.x - 1; x >= 0; x--) {
            for (int y = World.currentWorld.chunkSize.y - 1; y >= 0; y--) {
                for (int z = World.currentWorld.chunkSize.z - 1; z >= 0; z--) {
                    var currentBlock = GetBlockAt(x, y, z);

                    if (currentBlock == -1) continue;

                    var bottomBlock = GetBlockAt(x, y - 1, z);
                    var topBlock = GetBlockAt(x, y + 1, z);
                    var leftBlock = GetBlockAt(x - 1, y, z);
                    var rightBlock = GetBlockAt(x + 1, y, z);
                    var frontBlock = GetBlockAt(x, y, z + 1);
                    var backBlock = GetBlockAt(x, y, z - 1);

                    if (topBlock != -1) {
                        // dirt with grass born
                        if (topBlock == World.airDef) {
                            if (currentBlock == World.dirtDef) {
                                bool neighborHasGrass =
                                    (leftBlock != -1 && leftBlock == World.dirtWithGrassDef) ||
                                    (rightBlock != -1 && rightBlock == World.dirtWithGrassDef) ||
                                    (bottomBlock != -1 && bottomBlock == World.dirtWithGrassDef) ||
                                    (frontBlock != -1 && frontBlock == World.dirtWithGrassDef) ||
                                    (backBlock != -1 && backBlock == World.dirtWithGrassDef);

                                if (neighborHasGrass) {
                                    if (Random.Range(0, World.currentWorld.grassGrowProb) == World.currentWorld.grassGrowProb / 2) {
                                        SetBlockAt(x, y, z, World.dirtWithGrassDef, false);
                                        needsRegenerate = true;
                                    }
                                }
                            }
                        }
                        // dirt with grass death
                        else {
                            if (currentBlock == World.dirtWithGrassDef) {
                                if (Random.Range(0, World.currentWorld.grassDeathProb) == World.currentWorld.grassDeathProb / 2) {
                                    SetBlockAt(x, y, z, World.dirtDef, false);
                                    needsRegenerate = true;
                                }
                            }
                        }

                    }

                    if (bottomBlock != -1) {
                        if (bottomBlock == World.airDef) {
                            // apply gravity to block
                            if (World.currentWorld.blockDefs[currentBlock].useGravity) {
                                RemoveBlockAt(x, y, z, false);
                                Vector3Int pos = Vector3Int.FloorToInt(transform.position);
                                Vector3Int key = new Vector3Int(pos.x + x, pos.y + y, pos.z + z);
                                gravityBlocksToSpawn[key] = currentBlock;
                                needsRegenerate = true;
                            }
                            // water expand
                            if (currentBlock == World.waterDef) {
                                SetBlockAt(x, y - 1, z, World.waterDef, false);
                                needsRegenerate = true;
                            }
                        }
                    }
                    // if (topBlock != null && currentBlock.blockDef == World.waterDef && topBlock.blockDef == World.airDef) {
                    //     SetBlockAt(point + up, World.waterDef); // Agua não sobe né meu filho
                    //     needsRegenerate = true;
                    // }
                    if (leftBlock != -1 && currentBlock == World.waterDef && leftBlock == World.airDef) {
                        SetBlockAt(x - 1, y, z, World.waterDef, false);
                        needsRegenerate = true;
                    }
                    if (rightBlock != -1 && currentBlock == World.waterDef && rightBlock == World.airDef) {
                        SetBlockAt(x + 1, y, z, World.waterDef, false);
                        needsRegenerate = true;
                    }
                    if (frontBlock != -1 && currentBlock == World.waterDef && frontBlock == World.airDef) {
                        SetBlockAt(x, y, z + 1, World.waterDef, false);
                        needsRegenerate = true;
                    }
                    if (backBlock != -1 && currentBlock == World.waterDef && backBlock == World.airDef) {
                        SetBlockAt(x, y, z - 1, World.waterDef, false);
                        needsRegenerate = true;
                    }
                }
            }
        }

        if (needsRegenerate) {
            yield return RegenerateChunk();
        }
        if (gravityBlocksToSpawn.Count > 0) {
            var keys = gravityBlocksToSpawn.Keys;
            foreach (var key in keys) {
                World.currentWorld.InsertGravityBlockAt(key.x, key.y, key.z, gravityBlocksToSpawn[key]);
            }
        }
        CoroutinesRunning--;
        yield break;
    }

    private IEnumerator GenerateLightMap() {
        // int airBlockDef = World.currentWorld.FindBlockDefByName("Air");

        // for (int x = World.currentWorld.chunkSize.x - 1; x >= 0; x--) {
        //     for (int y = World.currentWorld.chunkSize.y - 1; y >= 0; y--) {
        //         for (int z = World.currentWorld.chunkSize.z - 1; z >= 0; z--) {
        //             BlockInfo block = map[x, y, z];

        //             if (y == World.currentWorld.chunkSize.y - 1) {
        //                 block.color = c_light;
        //                 block.isLightSource = true;
        //             } else {
        //                 if (IndexExists(x, y + 1, z)) {
        //                     BlockInfo upBlock = map[x, y + 1, z];
        //                     if (upBlock.isLightSource || upBlock.color.grayscale == 1.0f) {
        //                         if (World.currentWorld.blockDefs[block.blockDef].isLightTransparent) {
        //                             block.color = upBlock.color;
        //                         } else {
        //                             block.color = Color.Lerp(block.color, upBlock.color, 0.75f);
        //                         }
        //                     } else {
        //                         block.color = Color.Lerp(block.color, c_dark, 0.1f);
        //                     }
        //                 } else {
        //                     block.color = c_dark;
        //                 }
        //             }
        //         }
        //     }
        // }

        // for (int x = World.currentWorld.chunkSize.x - 1; x >= 0; x--) {
        //     for (int y = World.currentWorld.chunkSize.y - 1; y >= 0; y--) {
        //         for (int z = World.currentWorld.chunkSize.z - 1; z >= 0; z--) {
        //             BlockInfo block = map[x, y, z];
        //             Vector3Int point = new Vector3Int(x, y, z);

        //             if (block.color.grayscale < 1.0f) {
        //                 for (int a = -1; a < 1; a++) {
        //                     for (int b = -1; b < 1; b++) {
        //                         for (int c = -1; c < 1; c++) {
        //                             Vector3Int npos = new Vector3Int(point.x + a, point.y + b, point.z + c);

        //                             if (a != 0 && b != 0 && c != 0 && IndexExists(npos)) {
        //                                 BlockInfo neighbor = map[npos.x, npos.y, npos.z];
        //                                 if (neighbor.color.grayscale > block.color.grayscale) {
        //                                     block.color = Color.Lerp(block.color, neighbor.color, 0.45f);
        //                                 } else {
        //                                     block.color = Color.Lerp(block.color, c_dark, 0.1f);
        //                                 }
        //                             }
        //                         }
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }
        yield break;
    }

    public IEnumerator GenerateTrees() {
        int oakBarkDef = World.currentWorld.defIndexes["OakBark"];
        int oakLeavesDef = World.currentWorld.defIndexes["OakLeaves"];

        for (int x = World.currentWorld.chunkSize.x - 1; x >= 0; x--) {
            for (int y = World.currentWorld.chunkSize.y - 1; y > World.currentWorld.waterLevel; y--) {
                for (int z = World.currentWorld.chunkSize.z - 1; z >= 0; z--) {
                    if (map[x, y, z] != World.dirtWithGrassDef) continue;

                    if (Random.Range(0, 100) == 50) {
                        int treeBarkHeight = Random.Range(4, 8);
                        int treeTopWidth = 4;
                        int treeTopStart = 1 + treeBarkHeight / 2;
                        for (int a = 1; a < treeBarkHeight + 1; a++) {
                            if (a != treeBarkHeight) {
                                SetBlockAt(x, y + a, z, oakBarkDef, false);
                            }

                            if (a >= treeTopStart) {
                                int loopCount = (treeTopWidth - Mathf.Abs(a - treeTopStart)) / 2;
                                for (int b = -loopCount; b <= loopCount; b++) {
                                    for (int c = -loopCount; c <= loopCount; c++) {
                                        if (b == 0 && c == 0 && a != treeBarkHeight) continue;
                                        SetBlockAt(x + b, y + a, z + c, oakLeavesDef, false);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            yield return null;
        }
        yield break;
    }

    public IEnumerator GenerateMap() {
        map = new int[World.currentWorld.chunkSize.x, World.currentWorld.chunkSize.y, World.currentWorld.chunkSize.z];

        for (int x = 0; x < World.currentWorld.chunkSize.x; x++) {
            for (int y = 0; y < World.currentWorld.chunkSize.y; y++) {
                for (int z = 0; z < World.currentWorld.chunkSize.z; z++) {
                    map[x, y, z] = -1; // initialize with empty block

                    float xCoord = (World.currentWorld.usePositionAsOffset ? transform.position.x : 0.0f) / World.currentWorld.noiseScale + (World.currentWorld.noiseOffset.x + x / World.currentWorld.noiseScale);
                    float yCoord = (World.currentWorld.usePositionAsOffset ? transform.position.y : 0.0f) / World.currentWorld.noiseScale + (World.currentWorld.noiseOffset.y + y / World.currentWorld.noiseScale);
                    float zCoord = (World.currentWorld.usePositionAsOffset ? transform.position.z : 0.0f) / World.currentWorld.noiseScale + (World.currentWorld.noiseOffset.z + z / World.currentWorld.noiseScale);

                    float sample = Perlin.Fbm(xCoord, yCoord, zCoord, World.currentWorld.fractalLevel) / 1.4f + 0.5f;

                    if (sample < 0.2f) {
                        map[x, y, z] = World.airDef; // air
                    } else {
                        int heightmap = Mathf.RoundToInt(sample * World.currentWorld.chunkSize.y + (y / World.currentWorld.chunkSize.y));

                        if (y > heightmap) {
                            map[x, y, z] = World.airDef; // air
                        }
                        if (y == heightmap) {
                            if (sample <= 0.475f) {
                                map[x, y, z] = World.sandDef; // sand
                            } else {
                                map[x, y, z] = World.dirtWithGrassDef; // dirt-with-grass
                            }
                        }
                        if (y < heightmap) {
                            if (y == 0 || y <= Random.Range(0, 2)) {
                                map[x, y, z] = World.bedrockDef; // bedrock
                            }
                            if (y >= heightmap - Random.Range(1, 5) && y <= heightmap - 1) {
                                if (sample <= 0.4f) {
                                    map[x, y, z] = World.sandDef; // sand
                                } else {
                                    map[x, y, z] = World.dirtDef; // dirt
                                }
                            } else {
                                if (map[x, y, z] == -1) {
                                    map[x, y, z] = World.stoneDef; // stone
                                }
                                if (Random.Range(0, 100) == 21) {
                                    map[x, y, z] = World.diamondDef;

                                    for (int a = 0; a < Random.Range(1, 2); a++) {
                                        for (int b = 0; b < Random.Range(1, 2); b++) {
                                            if (IndexExists(x + a, y + b, z + a + b)) {
                                                map[x + a, y + b, z + a + b] = World.diamondDef;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            yield return null;
        }

        for (int x = 0; x < World.currentWorld.chunkSize.x; x++) {
            for (int y = 0; y < World.currentWorld.waterLevel; y++) {
                for (int z = 0; z < World.currentWorld.chunkSize.z; z++) {
                    Vector3Int position = new Vector3Int(x, y, z);
                    if (IndexExists(position) && IsAirBlock(position)) {
                        map[x, y, z] = World.waterDef;
                    }
                }
            }
        }
        yield break;
    }

    private IEnumerator GenerateMeshData() {
        for (int x = 0; x < World.currentWorld.chunkSize.x; x++) {
            for (int y = 0; y < World.currentWorld.chunkSize.y; y++) {
                for (int z = 0; z < World.currentWorld.chunkSize.z; z++) {;
                    int block = map[x, y, z];
                    if (block == World.airDef) continue;
                    DrawBlock(x, y, z, block);
                }
            }
        }
        yield break;
    }

    private IEnumerator GenerateChunkMesh() {
        t_mesh.Clear();
        t_mesh.vertices = verts.ToArray();

        var keys = tris.Keys;
        t_mesh.subMeshCount = keys.Count;

        foreach (var key in keys) {
            t_mesh.SetTriangles(tris[key].ToArray(), key);
        }

        t_mesh.uv = uv.ToArray();
        t_mesh.colors32 = colors.ToArray();
        t_mesh.RecalculateNormals();
        t_mesh.MarkModified();
        t_mesh.Optimize();

        t_meshRenderer.sharedMaterials = materials.ToArray();

        t_meshCollider.sharedMesh = t_mesh;
        yield break;
    }

    private void DrawBlock(Vector3 point, int block) {
        DrawBlock(Vector3Int.FloorToInt(point), block);
    }

    private void DrawBlock(Vector3Int point, int block) {
        DrawBlock(point.x, point.y, point.z, block);
    }

    private void DrawBlock(int x, int y, int z, int block) {
        Vector3 point = new Vector3(x, y, z);
        Vector3 right = new Vector3(1, 0, 0);
        Vector3 left = new Vector3(-1, 0, 0);
        Vector3 forward = new Vector3(0, 0, 1);
        Vector3 back = new Vector3(0, 0, -1);
        Vector3 up = new Vector3(0, 1, 0);
        Vector3 down = new Vector3(0, -1, 0);

        Color color = c_dark;
        BlockDef blockDef = World.currentWorld.blockDefs[block];

        int matIndex = blockDef.materialIndex;
        Material mat = World.currentWorld.materials[matIndex];

        if (materials.IndexOf(mat) == -1) {
            materials.Add(mat);
        }

        matIndex = materials.IndexOf(mat);

        var bottomBlock = GetBlockAt(x, y - 1, z);
        var topBlock = GetBlockAt(x, y + 1, z);
        var leftBlock = GetBlockAt(x - 1, y, z);
        var rightBlock = GetBlockAt(x + 1, y, z);
        var frontBlock = GetBlockAt(x, y, z + 1);
        var backBlock = GetBlockAt(x, y, z - 1);

        if (IsAirBlock(point + down) || (World.currentWorld.blockDefs[bottomBlock].isTransparent && block != bottomBlock)) {
            DrawFace(point + right, left, back, block, 0, color, matIndex); // bottom
        }
        if (IsAirBlock(point + up) || (World.currentWorld.blockDefs[topBlock].isTransparent && block != topBlock)) {
            if (block == World.waterDef && topBlock != -1 && topBlock != World.waterDef) { // is water
                DrawFace(point + new Vector3(0.0f, 0.875f, 0.0f), right, back, block, 1, color, matIndex); // top
            } else {
                DrawFace(point + up, right, back, block, 1, color, matIndex); // top
            }
        }
        if (IsAirBlock(point + right) || (World.currentWorld.blockDefs[rightBlock].isTransparent && block != rightBlock)) {
            if (block == World.waterDef && rightBlock != -1 && rightBlock != World.waterDef) { // is water
                DrawFace(point + right, back, new Vector3(0.0f, 0.875f, 0.0f), block, 2, color, matIndex); // right
            } else {
                DrawFace(point + right, back, up, block, 2, color, matIndex); // right
            }
        }
        if (IsAirBlock(point + left) || (World.currentWorld.blockDefs[leftBlock].isTransparent && block != leftBlock)) {
            if (block == World.waterDef && leftBlock != -1 && leftBlock != World.waterDef) { // is water
                DrawFace(point + back, forward, new Vector3(0.0f, 0.875f, 0.0f), block, 3, color, matIndex); // left
            } else {
                DrawFace(point + back, forward, up, block, 3, color, matIndex); // left
            }
        }
        if (IsAirBlock(point + forward) || (World.currentWorld.blockDefs[frontBlock].isTransparent && block != frontBlock)) {
            if (block == World.waterDef && frontBlock != -1 && frontBlock != World.waterDef) { // is water
                DrawFace(point, right, new Vector3(0.0f, 0.875f, 0.0f), block, 4, color, matIndex); // front
            } else {
                DrawFace(point, right, up, block, 4, color, matIndex); // front
            }
        }
        if (IsAirBlock(point + back) || (World.currentWorld.blockDefs[backBlock].isTransparent && block != backBlock)) {
            if (block == World.waterDef && backBlock != -1 && backBlock != World.waterDef) { // is water
                DrawFace(point + right + back, left, new Vector3(0.0f, 0.875f, 0.0f), block, 5, color, matIndex); // back
            } else {
                DrawFace(point + right + back, left, up, block, 5, color, matIndex); // back
            }
        }
    }

    private void DrawFace(Vector3 point, Vector3 offset1, Vector3 offset2, int block, int tileIndex, Color32 color, int meshIndex = 0) {
        point += Vector3.forward;

        int index = verts.Count;

        verts.Add(point);
        verts.Add(point + offset1);
        verts.Add(point + offset2);
        verts.Add(point + offset1 + offset2);

        if (!tris.ContainsKey(meshIndex) || tris[meshIndex] == null) {
            tris[meshIndex] = new List<int>();
        }

        tris[meshIndex].Add(index + 0);
        tris[meshIndex].Add(index + 1);
        tris[meshIndex].Add(index + 2);
        tris[meshIndex].Add(index + 3);
        tris[meshIndex].Add(index + 2);
        tris[meshIndex].Add(index + 1);

        for (int idx = 0; idx < 4; idx++) {
            colors.Add(color);
        }

        float textSize = 0.25f;

        float yCoord = World.currentWorld.blockDefs[block].tiles[tileIndex] / 4;
        float xCoord = yCoord * 4 + World.currentWorld.blockDefs[block].tiles[tileIndex];

        uv.Add(new Vector2(xCoord * textSize, yCoord * textSize));
        uv.Add(new Vector2(xCoord * textSize + textSize, yCoord * textSize));
        uv.Add(new Vector2(xCoord * textSize, yCoord * textSize + textSize));
        uv.Add(new Vector2(xCoord * textSize + textSize, yCoord * textSize + textSize));
    }
}