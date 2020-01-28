/*
    Author: Gus Tahara-Edmonds
    Date: Summer 2019
    Purpose: Decides the chunks to be generated. Capable of just making a grid or an infinite world. Calls the marching
    cubes compute shader to actually create the mesh.
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ChunkManager : MonoBehaviour {
    [Header("Size")]
    public int voxelsPerAxis;                           //number of voxels (i.e. "cubes") per axis per one chunk
    public float actualDimension;                       //physical dimensional size of one chunk (note: chunks are cube shaped)
    [Header("Shape")]       
    public float surfaceLevel;                          //the value that the numbers from the RNG must be greater than to be considered inside the mesh
    public bool smooth;                                 //whether or not to smooth out the faces
    public bool invert;                                 //whether or not to invert the world (i.e. floating islands become caves)
    [Header("View")]
    public bool infiniteWorld;                          //if enabled, will create an infinite world (place chunks around wherever the camera is)
    public int viewDistance;                            //how far away chunks will be generated (used only if infiniteWorld is enabled)
    public Vector3 fixedChunkDimensions;                //how many chunks to be generated in each direction (if infiniteWorld is disabled)
    [Header("LOD")]
    public LODProfile[] lodProfiles;                    //lod settings
    [Header("Misc")]
    public bool useCollisions;                          //whether or not the generated meshes should have collisions

    DensityBehaviour db;                                //script with the settings for the RNG
    ComputeShader marchCompute;

    //data buffers for holding density (value which determines what is inside mesh or outside, triangles, and the # of triangles
    RenderTexture densityData;
    ComputeBuffer triangleBuffer;
    ComputeBuffer triCountBuffer;

    int pointsPerAxis;                                  //number of vertices per axis per one chunk (i.e. a square is 1 face wide but 2 vertices wide)
    int threadsPerAxis;                                 //threads per axis per one chunk (used in compute shaders)
    int sqrViewDis;                                     //squared view distance is used because comparing distances is faster without square root
    bool generateOnUpdate;                              //whether or not world should be regenerated every frame (if infiniteWorld is enabled)
    new Camera camera;                                  //the main camera (where view distance is based from)

    //these hold references to the chunks and where they are (so program knows when they are out of view, etc.)
    List<ChunkInstance> chunks;
    List<Vector3> chunkCoords;
    Queue<ChunkInstance> chunksToRecycle = new Queue<ChunkInstance>();      //Queue used when recycling chunks that got out of view (so don't have to be constantly destroyed/recreated)

    void Start() {
        if (Mathf.Round(voxelsPerAxis / 8f) != voxelsPerAxis / 8f) {
            Debug.LogError("Voxels Per Axis must be divisible by 8");
        }

        if(infiniteWorld) {
            generateOnUpdate = true;
        }

        pointsPerAxis = voxelsPerAxis + 1;
        threadsPerAxis = pointsPerAxis / 8;
        sqrViewDis = viewDistance * viewDistance;
        camera = Camera.main;
        chunks = new List<ChunkInstance>();
        chunkCoords = new List<Vector3>();

        marchCompute = Resources.Load("MarchingCubes") as ComputeShader;

        //compute shaders take a bit of setting up (i.e. sending values from this script to GPU)
        InitDensityBehaviour();
        InitMarchingCubesCompute();

        if (infiniteWorld) {
            GenerateVisibleChunks();
        }
        else {
            GenerateFixedChunks();
        }
    }

    void Update() {
        pointsPerAxis = voxelsPerAxis + 1;

        //Pressing "G"
        if (Input.GetKeyDown(KeyCode.G)) {
            float startTime = Time.realtimeSinceStartup;
            if (infiniteWorld) {
                GenerateVisibleChunks();
            }
            else {
                GenerateFixedChunks();
            }
            Debug.Log("Loaded in " + (Time.realtimeSinceStartup - startTime) + " Seconds.");
        }

        //Pressing "T" sends data to the compute shaders (if values changed in inspector) *not super reliable if certain variables' values are changed
        if(Input.GetKeyDown(KeyCode.T)) {
            float startTime = Time.realtimeSinceStartup;
            UpdateMarchingCubesParams();
            Debug.Log("Loaded in " + (Time.realtimeSinceStartup - startTime) + " Seconds.");
        }

        //Pressing "F" toggles whether infinite world is being updated based on camera position
        if(Input.GetKeyDown(KeyCode.F)) {
            generateOnUpdate = !generateOnUpdate;
        }

        if(generateOnUpdate) {
            GenerateVisibleChunks();
        }
    }

    #region Chunk generators
    //handles which chunks should be created, updated, or destroyed if infiniteWorld is enabled 
    void GenerateVisibleChunks() {
        Vector3 p = camera.transform.position;
        Vector3 pCoord = WorldPos2Coord(p);

        //iterates through the currently active chunks to find which ones need to be recycled (if in the wrong LOD for it's position or too far away to be seen)
        //if does need to be recycled, add them to the queue to be used later
        foreach (ChunkInstance c in chunks) {
            float sqrDis = Vector3.SqrMagnitude(ChunkPos2Centre(c.coord) - p);
            if (sqrDis > sqrViewDis || GetLODProfile(sqrDis) != c.lodIndex) {
                chunksToRecycle.Enqueue(c);
                chunkCoords.Remove(c.coord); 
            }
        }

        //runs through the possible chunk positions. if there is a chunk at said position, move to the next. if there isn't, add one.
        int maxChunksInView = Mathf.CeilToInt(viewDistance / (float)actualDimension);
        for (int x = -maxChunksInView; x <= maxChunksInView; x++) {
            for (int y = -maxChunksInView; y <= maxChunksInView; y++) {
                for (int z = -maxChunksInView; z <= maxChunksInView; z++) {
                    Vector3 worldPos = pCoord + new Vector3(x, y, z) * actualDimension;

                    if(chunkCoords.Contains(worldPos)) {
                        continue;
                    }

                    //make sure that chunk is close enough and also in the direction the player is facing. otherwise it doesn't need to be generated
                    Vector3 centre = ChunkPos2Centre(worldPos);
                    float sqrDis = Vector3.SqrMagnitude(centre - p);
                    if (sqrDis <= sqrViewDis) {
                        Bounds bounds = new Bounds(centre, Vector3.one * actualDimension);
                        if (IsVisible(bounds)) { 
                            //now run the RNG to generate random terrain for our point
                            db.Generate(threadsPerAxis + 1, worldPos);
                            int lodProfile = GetLODProfile(sqrDis);

                            //if we still have chunk GameObjects that were created previously and needed to be recycled, use one. if we don't, create a new GameObject.
                            if (chunksToRecycle.Count > 0) {
                                ChunkInstance c = chunksToRecycle.Dequeue();
                                c.name = GetName(worldPos);
                                c.transform.position = transform.position + worldPos;
                                c.coord = worldPos;
                                c.lodIndex = lodProfile;
                                GenerateChunkMesh(c);
                                chunkCoords.Add(worldPos);
                            }
                            else {
                                ChunkInstance c = CreateNewChunk(worldPos, lodProfile);
                                GenerateChunkMesh(c);
                                chunks.Add(c);
                                chunkCoords.Add(worldPos);
                            }
                        }
                    }
                }
            }
        }

        //if any chunks that were recycled and didn't end up being used, they can be destroyed
        while(chunksToRecycle.Count > 0) {
            ChunkInstance c = chunksToRecycle.Dequeue();
            chunks.Remove(c);
            Destroy(c.gameObject);
        }
    }

    //if infinite world is disabled, generate the simple grid of chunks
    void GenerateFixedChunks() {
        for (int x = 0; x < fixedChunkDimensions.x; x++) {
            for (int y = 0; y < fixedChunkDimensions.y; y++) {
                for (int z = 0; z < fixedChunkDimensions.z; z++) {
                    Vector3 worldPos = new Vector3(x, y, z) * actualDimension;
                    db.Generate(threadsPerAxis + 1, worldPos);
                    ChunkInstance c = CreateNewChunk(worldPos, 0);
                    GenerateChunkMesh(c);
                }
            }
        }
    }
    #endregion

    #region Chunk gen helper functions
    //create a new chunk GameObject. needs to be passed certain values and also be made a child of this object (for neatness)
    ChunkInstance CreateNewChunk(Vector3 worldPos, int lodProfile) {
        GameObject newChunk = new GameObject(GetName(worldPos));
        newChunk.transform.parent = transform;
        newChunk.transform.position = worldPos + transform.position;
        ChunkInstance c = newChunk.AddComponent<ChunkInstance>();
        c.Setup(worldPos, lodProfile, useCollisions);
        return c;
    }

    //finds the distance between a chunks coordinate and its actual centre
    //i.e. a chunk with coordinate (0,0,0) will have one corner at (0,0,0) but the chunk's whole centre is offset
    Vector3 ChunkPos2Centre(Vector3 coord) {
        return coord + Vector3.one * actualDimension / 2;
    }

    //the chunk coordinate that cooresponds to a given world position
    Vector3 WorldPos2Coord(Vector3 worldPos) {
        return actualDimension * new Vector3(Mathf.Round(worldPos.x / actualDimension), Mathf.Round(worldPos.y / actualDimension), Mathf.Round(worldPos.z / actualDimension));
    }

    //used when naming the chunk instances
    string GetName(Vector3 pos) {
        return "Chunk at (" + pos.x + ", " + pos.y + ", " + pos.z + ")";
    }

    //calculate if a chunk is visible based on a given camera orientation
    public bool IsVisible(Bounds bounds) {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, bounds);
    }

    //get the lod setting that cooresponds to a given squared distance value. sqared distance used as square root is expensive
    int GetLODProfile(float sqrDis) {
        for(int i = 0; i < lodProfiles.Length; i++) {
            float lodDis = lodProfiles[i].maxActiveDistance;
            if (sqrDis < lodDis * lodDis) {
                return i;
            }
        }

        return lodProfiles.Length - 1;
    }
    #endregion

    #region Compute shader init
    //setup the render texture which holds the RNG values. this is where the compute shader that generates our random world saves its output to.
    void InitDensityBehaviour() {
        densityData = new RenderTexture(pointsPerAxis, pointsPerAxis, 0) {
            format = RenderTextureFormat.RFloat,
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = pointsPerAxis,
            enableRandomWrite = true
        };
        densityData.Create();

        db = GetComponent<DensityBehaviour>();
        db.Init(densityData, pointsPerAxis, actualDimension / voxelsPerAxis);
    }

    //send all necessary values from this script to the marching cubes compute shader
    void InitMarchingCubesCompute() {
        int maxNumTriangles = voxelsPerAxis * voxelsPerAxis * voxelsPerAxis * 5;
        triangleBuffer = new ComputeBuffer(maxNumTriangles, sizeof(float) * 3 * 3, ComputeBufferType.Append);
        triCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        marchCompute.SetInt("pointsPerAxis", pointsPerAxis);
        marchCompute.SetFloat("scale", actualDimension / voxelsPerAxis);
        UpdateMarchingCubesParams();
    }

    //update values from this script to the marching cubes compute shader settings
    void UpdateMarchingCubesParams() {
        marchCompute.SetFloat("surfaceLevel", surfaceLevel);
        marchCompute.SetBool("smooth", smooth);
        marchCompute.SetBool("invert", invert);
        marchCompute.SetTexture(0, "densityData", densityData);
        marchCompute.SetBuffer(0, "triangles", triangleBuffer);
        marchCompute.SetInt("lodModifier", 1);
    }
    #endregion

    #region Single chunk updaters
    //method which calls the marching cubes algorithm and recieves its data to create the mesh for a given chunk
    void GenerateChunkMesh(ChunkInstance c) {
        //basic stuff: get the lod, set the material to be right one for its lod level
        int lodModifier = lodProfiles[c.lodIndex].lodModifier;
        c.SetMat(lodProfiles[c.lodIndex].mat);
        marchCompute.SetInt("lodModifier", lodModifier);

        //init buffers so data can be properly collected later
        triangleBuffer.SetCounterValue(0);
        int threads = Mathf.Max(threadsPerAxis / lodModifier, 1);
        marchCompute.Dispatch(0, threads, threads, threads);

        //get length of triangles to be made
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData(triCountArray);
        int numTris = triCountArray[0];

        //retrieve triangle data
        Triangle[] triangles = new Triangle[numTris];
        triangleBuffer.GetData(triangles, 0, 0, numTris);

        //get the vertices and triangles for the mesh
        Vector3[] vertices = new Vector3[numTris * 3];
        int[] meshTriangles = new int[numTris * 3];
        for (int i = 0; i < numTris; i++) {
            for (int j = 0; j < 3; j++) {
                meshTriangles[i * 3 + j] = i * 3 + j;
                vertices[i * 3 + j] = triangles[i][j];
            }
        }

        c.mesh.Clear();
        c.mesh.vertices = vertices;
        c.mesh.triangles = meshTriangles;
        c.mesh.RecalculateNormals();

        if(useCollisions) {
            c.collider.enabled = false;
            c.collider.enabled = true;
        }
    }
    #endregion

    #region Buffer management
    void OnDestroy() {
        if (Application.isPlaying) {
            ReleaseBuffers();
        }
    }

    void ReleaseBuffers() {
        if (triangleBuffer != null) {
            densityData.Release();
            triangleBuffer.Release();
            triCountBuffer.Release();
        }
    }
    #endregion

    #region structs/classes
    //used in inspector to nicely format lod settings
    [System.Serializable]
    public class LODProfile {
        public float maxActiveDistance;                 //max distance at which this lod is used
        public int lodModifier;                         //how the mesh is modified at this lod. an lod of 2 means 1/2 resolution, lod of 4 means 1/4 resolution, etc. 
        public Material mat;                            //material to be applied at this lod
    }

    //struct to contain the data for a triangle (necessary for sending recieving data properly from compute shader as far as I know)
    struct Triangle {
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;

        public Vector3 this[int i] {
            get {
                switch (i) {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }
    #endregion
}
