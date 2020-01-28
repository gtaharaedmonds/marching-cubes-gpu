/*
    Author: Gus Tahara-Edmonds
    Date: Summer 2019
    Purpose: Used to communicate and hold some data for one specific chunk. Nothing too complicated.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkInstance : MonoBehaviour {
    public Mesh mesh;
    public MeshRenderer rend;
    public Vector3 coord;
    public int lodIndex;
    public MeshCollider collider;

    //when this chunk is created, function is called to make sure chunk is setup properly
    public void Setup(Vector3 worldPos, int lodModifier, bool useCollisions) {
        coord = worldPos;
        this.lodIndex = lodModifier;

        rend = gameObject.AddComponent<MeshRenderer>();
        MeshFilter filter = gameObject.AddComponent<MeshFilter>();

        mesh = new Mesh {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        filter.mesh = mesh;

        if (useCollisions) {
            collider = gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
        }
    }

    //sets the material this chunk uses to render
    public void SetMat(Material mat) {
        rend.material = mat;
    }
}
