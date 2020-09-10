using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class CloudsChunk : MonoBehaviour {

    public int CoroutinesRunning = 0;

    [Header("Mesh Data")]
    List<Vector3> verts = new List<Vector3>();
    List<int> tris = new List<int>();
    List<Vector2> uv = new List<Vector2>();

    int[, ] map;

    Mesh t_mesh;
    MeshFilter t_meshFilter;
    MeshRenderer t_meshRenderer;

    void Start() {
        t_meshRenderer = gameObject.AddComponent<MeshRenderer>();

        t_meshFilter = gameObject.AddComponent<MeshFilter>();
        t_mesh = new Mesh();
        t_mesh.name = $"Chunk {t_mesh.GetInstanceID()}";
        t_mesh.MarkDynamic();

        t_meshFilter.mesh = t_mesh;

        // GenerateChunk();
        StartCoroutine(GenerateChunk());
    }

    public int GetBlockAt(Vector3 point) {
        return GetBlockAt(Vector3Int.FloorToInt(point));
    }

    public int GetBlockAt(Vector3Int point) {
        return GetBlockAt(point.x, point.z);
    }

    public int GetBlockAt(int x, int z) {
        if (IndexExists(x, z)) {
            return map[x, z];
        } else {
            return -1;
        }
    }

    public bool IsAirBlock(Vector3 point) {
        return IsAirBlock(Vector3Int.FloorToInt(point));
    }

    public bool IsAirBlock(Vector3Int point) {
        return IsAirBlock(point.x, point.y, point.z);
    }

    public bool IsAirBlock(int x, int y, int z) {
        return (!IndexExists(x, z)) || map[x, z] == 0;
    }

    public bool IndexExists(Vector3 point) {
        return IndexExists(Vector3Int.FloorToInt(point));
    }

    public bool IndexExists(Vector3Int point) {
        return IndexExists(point.x, point.z);
    }

    public bool IndexExists(int x, int z) {
        return !(x < 0 || x >= World.currentWorld.chunkSize.x || z < 0 || z >= World.currentWorld.chunkSize.z);
    }

    public virtual IEnumerator GenerateChunk() {
        CoroutinesRunning++;
        yield return ResetChunk();
        yield return GenerateMap();
        yield return GenerateMeshData();
        yield return GenerateChunkMesh();
        CoroutinesRunning--;
        yield break;
    }

    public IEnumerator ResetChunk() {
        // t_meshCollider.sharedMesh = null;
        // t_meshFilter.mesh = null;
        verts.Clear();
        tris.Clear();
        uv.Clear();
        yield break;
    }

    public IEnumerator GenerateMap() {
        map = new int[World.currentWorld.chunkSize.x, World.currentWorld.chunkSize.z];

        for (int x = 0; x < World.currentWorld.chunkSize.x; x++) {
            for (int z = 0; z < World.currentWorld.chunkSize.z; z++) {
                map[x, z] = -1; // initialize with empty block

                float xCoord = (World.currentWorld.usePositionAsOffset ? transform.position.x : 0.0f) / World.currentWorld.noiseScale + (World.currentWorld.noiseOffset.x + x / World.currentWorld.noiseScale);
                float zCoord = (World.currentWorld.usePositionAsOffset ? transform.position.z : 0.0f) / World.currentWorld.noiseScale + (World.currentWorld.noiseOffset.z + z / World.currentWorld.noiseScale);

                float a = Perlin.Fbm(xCoord, zCoord, World.currentWorld.fractalLevel) / 1.4f + 0.5f;
                float b = Perlin.Fbm(zCoord, xCoord, 3) / 1.4f + 0.5f;

                if (Mathf.Lerp(a, b, 0.5f) < 1.0f - World.currentWorld.cloudsCoverage) {
                    map[x, z] = World.airDef;
                } else {
                    map[x, z] = 1;
                }
            }
            yield return null;
        }

        yield break;
    }

    private IEnumerator GenerateMeshData() {
        for (int x = 0; x < World.currentWorld.chunkSize.x; x++) {
            for (int z = 0; z < World.currentWorld.chunkSize.z; z++) {;
                int block = map[x, z];
                if (block == World.airDef) continue;
                DrawBlock(x, z, block);
            }
        }
        yield break;
    }

    private IEnumerator GenerateChunkMesh() {
        t_mesh.Clear();

        t_mesh.vertices = verts.ToArray();
        t_mesh.triangles = tris.ToArray();
        t_mesh.uv = uv.ToArray();

        t_mesh.RecalculateNormals();
        t_mesh.MarkModified();
        t_mesh.Optimize();

        t_meshRenderer.sharedMaterial = World.currentWorld.cloudsMaterial;

        yield break;
    }

    private void DrawBlock(Vector3 point, int block) {
        DrawBlock(Vector3Int.FloorToInt(point), block);
    }

    private void DrawBlock(Vector3Int point, int block) {
        DrawBlock(point.x, point.z, block);
    }

    private void DrawBlock(int x, int z, int block) {
        Vector3 point = new Vector3(x, 0, z);
        Vector3 right = new Vector3(1, 0, 0);
        Vector3 left = new Vector3(-1, 0, 0);
        Vector3 forward = new Vector3(0, 0, 1);
        Vector3 back = new Vector3(0, 0, -1);
        Vector3 up = new Vector3(0, 1, 0);
        Vector3 down = new Vector3(0, -1, 0);

        var leftBlock = GetBlockAt(x - 1, z);
        var rightBlock = GetBlockAt(x + 1, z);
        var frontBlock = GetBlockAt(x, z + 1);
        var backBlock = GetBlockAt(x, z - 1);

        DrawFace(point + right, left, back); // bottom
        DrawFace(point + up, right, back); // top

        if (IsAirBlock(point + right) || (World.currentWorld.blockDefs[rightBlock].isTransparent && block != rightBlock)) {
            if (block == World.waterDef && rightBlock != -1 && rightBlock != World.waterDef) { // is water
                DrawFace(point + right, back, new Vector3(0.0f, 0.875f, 0.0f)); // right
            } else {
                DrawFace(point + right, back, up); // right
            }
        }
        if (IsAirBlock(point + left) || (World.currentWorld.blockDefs[leftBlock].isTransparent && block != leftBlock)) {
            if (block == World.waterDef && leftBlock != -1 && leftBlock != World.waterDef) { // is water
                DrawFace(point + back, forward, new Vector3(0.0f, 0.875f, 0.0f)); // left
            } else {
                DrawFace(point + back, forward, up); // left
            }
        }
        if (IsAirBlock(point + forward) || (World.currentWorld.blockDefs[frontBlock].isTransparent && block != frontBlock)) {
            if (block == World.waterDef && frontBlock != -1 && frontBlock != World.waterDef) { // is water
                DrawFace(point, right, new Vector3(0.0f, 0.875f, 0.0f)); // front
            } else {
                DrawFace(point, right, up); // front
            }
        }
        if (IsAirBlock(point + back) || (World.currentWorld.blockDefs[backBlock].isTransparent && block != backBlock)) {
            if (block == World.waterDef && backBlock != -1 && backBlock != World.waterDef) { // is water
                DrawFace(point + right + back, left, new Vector3(0.0f, 0.875f, 0.0f)); // back
            } else {
                DrawFace(point + right + back, left, up); // back
            }
        }
    }

    private void DrawFace(Vector3 point, Vector3 offset1, Vector3 offset2) {
        point += Vector3.forward;

        int index = verts.Count;

        verts.Add(point);
        verts.Add(point + offset1);
        verts.Add(point + offset2);
        verts.Add(point + offset1 + offset2);

        tris.Add(index + 0);
        tris.Add(index + 1);
        tris.Add(index + 2);
        tris.Add(index + 3);
        tris.Add(index + 2);
        tris.Add(index + 1);
    }
}