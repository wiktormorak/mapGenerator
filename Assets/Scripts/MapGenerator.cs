using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class MapGenerator : MonoBehaviour
{
    const float XConstant = 0.01716f;
    const float ZConstant = 0.01483f;
    const float ChunkXConstant = 0.515f;
    const float ChunkZConstant = 0.445f;
    [Header("Prefab Configuration")]
    #region Prefab Configuration
    public GameObject tilePrefab;
    private Vector3 tilePrefabSize;
    public Vector3 tileSize;
    public Vector2 tilePadding;
    public float tileDistanceXAxis;
    public float tileDistanceZAxis;
    #endregion
    [Header("Map Configuration")]
    #region Configuration Variables
    public GameObject container;
    public bool isContinuous;
    public Vector3 mapSize;
    public Vector3 mapSizeInChunks;
    public int seed;
    public ChunkDetail chunkDetail;
    public float chunkDistanceXAxis;
    public float chunkDistanceZAxis;
    #endregion
    [Header("Map Data")]
    #region Map Data
    public Vector3 chunkSize;
    public float totalChunks;
    public float totalRows;
    public float totalColumns;
    private int tileCount;
    private int cIndex;
    private List<GameObject> allChunks = new List<GameObject>();
    private List<GameObject> tilesUnsorted = new List<GameObject>();
    private List<Renderer> tileRenderers = new List<Renderer>();
    public List<GameObject> indexedBiomes = new List<GameObject>();
    #endregion
    #region Chunk Generation
    private int chunkIndex;
    private int chunkRow;
    private int chunkColumn;
    private float chunksToProcess;
    private float tilesPerChunk;
    private float rowsPerChunk;
    private float offset;
    private bool offsetRow;
    private bool chunksComplete;
    private Vector3 lastChunkPosition;
    #endregion
    [Header("Biome Configuration")]
    #region Biomes
    public List<Biome> biomes;
    public List<Biome.BiomeSpawnData> biomeSpawnData;
    private float cachedTemperature;
    private float lastTemperature;
    private float minimumBiomeTemperature;
    private float maximumBiomeTemperature;
    public Biome initialBiome;
    public Biome lastBiome;
    public Biome currentBiome;
    public int biomeIndex;
    private Material currentBiomeMaterial;
    private int currentBiomeMaxSize;
    private int currentBiomeMinSize;
    private int currentBiomeMaxSizeInChunks;
    private int currentBiomeMinSizeInChunks;
    private float currentBiomeMaxWidth;
    private float currentBiomeMinWidth;
    private float currentBiomeMaxLength;
    private float currentBiomeMinLength;
    private float cachedBiomeSize;
    private float biomeSpawnRandom;
    #endregion
    #region Rendering
    private List<BatchRendererGroup> brgs;
    private Mesh tileMesh;
    private List<List<BatchID>> batchIDs;
    private List<List<BatchMaterialID>> batchMaterialIDs;
    private List<List<Mesh>> meshLists;
    private List<List<GraphicsBuffer>> transformBuffers = new List<List<GraphicsBuffer>>();
    #endregion
    #region Unity Methods & Un-Important
    void Start()
    {
        Invoke(nameof(InvokeAtStart), 1);
    }
    void Update()
    {
        if (chunksToProcess > 0){
            ChunkScheduler();
        }
    }
    void InvokeAtStart() {
        tileMesh = tilePrefab.GetComponent<MeshFilter>().sharedMesh;
        StoreBiomeRanges();
        GenerateSeed();
        tileDistanceXAxis = FindTileGapXAxis(tileSize.x);
        tileDistanceZAxis = FindTileGapZAxis(tileSize.x);
        GetMapSize();
        GetChunkDetailPercent();
        chunkDistanceXAxis = FindChunkGapXAxis(chunkSize.x);
        chunkDistanceZAxis = FindChunkGapZAxis(chunkSize.z);
        GetTotalChunks();
        mapSizeInChunks = new Vector3(Mathf.Round((mapSize.x / chunkSize.x)), Mathf.Round((mapSize.y / chunkSize.y)), Mathf.Round((mapSize.z / chunkSize.z)));
        initialBiome = SetInitialBiome();
        chunksToProcess += totalChunks;
    }
    bool IsEven(int value) {
        return (value & 1) == 0;
    }
    bool IsOdd(int value) {
        return (value & 1) == 1;
    }
    float RoundToThird(float value) {
        value = Mathf.Floor((value * 1000) / 1000);
        return value;
    }
    #endregion
    #region important Methods
    void GenerateSeed() {
        seed = Random.Range(-2147483647, 2147483647);
        Random.InitState(seed);
    }
    void GetMapSize() {
        totalRows = mapSize.x;
        totalColumns = mapSize.z;
    }
    void GetChunkDetailPercent() {
        if(chunkDetail == ChunkDetail.tiny){
            chunkSize.x = (((totalRows / totalColumns) * 10f) * 0.3f);
            chunkSize.y = (((totalRows / totalColumns) * 10f) * 0.3f);
            chunkSize.z = (((totalRows / totalColumns) * 10f) * 0.3f);
        }
        else if(chunkDetail == ChunkDetail.small){
            chunkSize.x = (((totalRows / totalColumns) * 10f) * 0.5f);
            chunkSize.y = (((totalRows / totalColumns) * 10f) * 0.5f);
            chunkSize.z = (((totalRows / totalColumns) * 10f) * 0.5f);
        }
        else if(chunkDetail == ChunkDetail.medium){
            chunkSize.x = (((totalRows / totalColumns) * 10f) * 0.7f);
            chunkSize.y = (((totalRows / totalColumns) * 10f) * 0.7f);
            chunkSize.z = (((totalRows / totalColumns) * 10f) * 0.7f);
        }
        else{
            chunkSize.x = (((totalRows / totalColumns) * 10f) * 0.9f);
            chunkSize.y = (((totalRows / totalColumns) * 10f) * 0.9f);
            chunkSize.z = (((totalRows / totalColumns) * 10f) * 0.9f);
        }
    }
    void GetTotalChunks(){
        totalChunks = (mapSize.x / chunkSize.x) * (mapSize.z / chunkSize.z);
        tilesPerChunk = (chunkSize.x * chunkSize.z);
        rowsPerChunk = (tilesPerChunk / chunkSize.x);
    }
    float FindTileGapXAxis(float size) {
        float tileGap = (size * XConstant);
        return tileGap;
    }
    float FindTileGapZAxis(float size) {
        float tileGap = (size * ZConstant);
        return tileGap;
    }
    float FindChunkGapXAxis(float size) {
        float chunkGap = (size * ChunkXConstant);
        return chunkGap;
    }
    float FindChunkGapZAxis(float size) {
        float chunkGap = (size * ChunkZConstant);
        return chunkGap;
    }
    #endregion
    #region Chunk Generation Methods
    GameObject CreateChunkGameObject(int index) {
        GameObject chunkParent = new GameObject();
        chunkParent.AddComponent<ChunkData>();
        chunkParent.GetComponent<ChunkData>().chunkIndex = index;
        chunkParent.transform.localScale = chunkSize;
        return chunkParent;
    }
    void ChunkScheduler() {
        if (chunksToProcess > 0){
            for (int i = 0; i < chunksToProcess; i++) {
                GameObject chunkParent = CreateChunkGameObject(cIndex);
                #region First Chunk
                if (cIndex == 0){
                    chunkParent.GetComponent<ChunkData>().chunkBiome = initialBiome;
                    SetChunkTileMaterial(chunkParent);
                    cachedBiomeSize = Random.Range(currentBiomeMinWidth, currentBiomeMaxWidth);
                    Debug.Log(currentBiomeMinWidth);
                    Debug.Log(currentBiomeMaxWidth);
                    Debug.Log(cachedBiomeSize);
                }
                #endregion
                Biome chunkBiome = GetNextBiome(chunkParent);
                chunkParent.name = lastTemperature.ToString();
                SetChunkBiome(chunkParent,  chunkBiome);
                chunkRow++;
                #region Change Column
                if (Mathf.Approximately(chunkRow, cachedBiomeSize)) {
                    chunkRow = 0;
                    chunkColumn++;
                }
                #endregion
                bool chunkOffset = (chunkColumn & 1) == 1;
                GenerateChunk(chunkParent, chunkOffset, cIndex);
                chunkParent.transform.position = SetChunkPosition();
                SetChunkTileMaterial(chunkParent);
                cIndex++;
                chunksToProcess--;
            }
        }
    }
    Vector3 SetChunkPosition() {
        Vector3 chunkPos = new Vector3(chunkSize.x + (chunkDistanceXAxis * chunkRow), 0f, chunkSize.z + (chunkDistanceZAxis * chunkColumn));
        return chunkPos;
    }
    void SetChunkParent(GameObject chunk) {
        chunk.transform.SetParent(container.transform);
    }
    void GenerateChunk(GameObject parent, bool chunkInverse, int index) {
        for (int i = 0; i < rowsPerChunk; i++){
            chunkIndex++;
            if (!chunkInverse){
                offsetRow = (i & 1) == 1;
            }
            else{
                offsetRow = (i & 1) == 0;
            }
            CreateRow(parent, offsetRow, i);
            offsetRow = false;
        }
    }
    void CreateRow(GameObject parent, bool rowOffset, int rowIndex) {
        for (int i = 0; i < rowsPerChunk; i++){
            float gapX = tileDistanceXAxis;
            float gapZ = tileDistanceZAxis;
            if (rowOffset){
                offset = gapX / 2f;
            }
            else{offset = 0f;}
            Vector3 tilePos = new Vector3((gapX * i) + offset , 0f, gapZ * rowIndex);
            CreateTile(parent, tilePos);
            offset = 0f;
        }
    }
    void CreateTile(GameObject parent, Vector3 pos){
        GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);
        //Mesh tileMesh = tile.GetComponent<MeshFilter>().sharedMesh;
        tile.transform.localScale = tileSize;
        tile.transform.eulerAngles = new Vector3(-90f,0f,0f);
        tile.transform.SetParent(parent.transform);
    }
    #endregion
    #region Biome Utility Methods
    void AddChunkToBiomeFromIndex(GameObject parent, GameObject chunk) {
        chunk.transform.SetParent(parent.transform);
    }
    void AddChunksToBiomeFromIndex(int index, List<GameObject> chunks) {
        GameObject biome = indexedBiomes[index];
        foreach (GameObject chunk in chunks){
            chunk.transform.SetParent(biome.transform);
        }
    }
    void CreateBiomeGameObject(Biome biomeType, int index) {
        GameObject biomeParent = new GameObject("Biome " + index.ToString() + "(" + biomeType.name + ")");
        biomeParent.transform.SetParent(container.transform);
        biomeParent.AddComponent<BiomeData>();
        biomeParent.GetComponent<BiomeData>().biome = biomeType;
        biomeParent.GetComponent<BiomeData>().biomeMaterial = biomeType.biomeSpawnData.tileMaterial;
        AddToIndexedBiomes(biomeParent);
    }
    void AddToIndexedBiomes(GameObject obj) {
        indexedBiomes.Add(obj);
    }
    #endregion
    #region Biome Methods
    void StoreBiomeRanges() {
        foreach (Biome biome in biomes){
            biomeSpawnData.Add(biome.biomeSpawnData);
        }
    }
    float BiomeMinMaxRandom() {
        var i = Random.Range(0f, 2.99f);
        return i;
    }
    Biome SetInitialBiome() {
        biomeSpawnRandom = BiomeMinMaxRandom();
        //biomeSpawnRandom = RoundToThird(biomeSpawnRandom);
        lastTemperature = biomeSpawnRandom;
        for (int i = 0; i < biomeSpawnData.Count; i++) {
            var range = biomeSpawnData[i];
            if (biomeSpawnRandom >= range.minChanceSpawn && biomeSpawnRandom <= range.maxChanceSpawn){
                StoreBiomeData(i);
                initialBiome = biomes[i];
                lastBiome = initialBiome;
                biomeIndex = 0;
                CreateBiomeGameObject(initialBiome, biomeIndex);
                return initialBiome;
            }
        }
        return null;
    }
    Biome GetNextBiome(GameObject chunk) {
        float divider = Random.Range(15f, 75f);
        if (lastTemperature > 0f) {
            lastTemperature -= (((Random.Range(minimumBiomeTemperature, maximumBiomeTemperature) / divider) * Random.Range(currentBiomeMinLength, currentBiomeMaxLength) / divider) / 10f);
        }
        else{
            lastTemperature = BiomeMinMaxRandom();
        }
        //Debug.Log((((Random.Range(minimumBiomeTemperature, maximumBiomeTemperature) * divider) / ((currentBiomeMinLength * currentBiomeMaxLength)) * divider) / 100f));
        //lastTemperature = RoundToThird(lastTemperature);
        chunk.GetComponent<ChunkData>().chunkTemperature = lastTemperature;
        chunk.name = lastTemperature.ToString();
        for (int i = 0; i < biomeSpawnData.Count; i++) {
            var range = biomeSpawnData[i];
            if (lastTemperature >= range.minChanceSpawn && lastTemperature <= range.maxChanceSpawn){
                currentBiome = biomes[i];
                if (currentBiome != lastBiome){
                    lastBiome = currentBiome;
                    StoreBiomeData(i);
                    currentBiome = biomes[i];
                    GameObject parent = indexedBiomes[biomeIndex];
                    CreateBiomeGameObject(currentBiome, biomeIndex);
                    AddChunkToBiomeFromIndex(parent, chunk);
                    chunk.GetComponent<ChunkData>().chunkTemperatureDivider = divider;
                    biomeIndex++;
                    return currentBiome;
                }
                else if(currentBiome == lastBiome){
                    lastBiome = currentBiome;
                    GameObject parent = indexedBiomes[biomeIndex];
                    AddChunkToBiomeFromIndex(parent, chunk);
                    chunk.GetComponent<ChunkData>().chunkTemperatureDivider = divider;
                    return currentBiome;
                }
            }
        }
        return null;
    }
    void SetChunkTileMaterial(GameObject chunk) {
        foreach (Transform child in chunk.transform) {
            child.GetComponent<Renderer>().material = currentBiomeMaterial;
        }
    }
    void SetChunkBiome(GameObject chunk, Biome biome) {
        chunk.GetComponent<ChunkData>().chunkBiome = biome;
    }
    void StoreBiomeData(int i) {
        minimumBiomeTemperature = biomeSpawnData[i].minChanceSpawn;
        //minimumBiomeTemperature = RoundToThird(minimumBiomeTemperature);
        maximumBiomeTemperature = biomeSpawnData[i].maxChanceSpawn;
        //maximumBiomeTemperature = RoundToThird(maximumBiomeTemperature);
        currentBiomeMaterial = biomeSpawnData[i].tileMaterial;
        currentBiomeMaxSizeInChunks = (currentBiomeMaxSize / (int)mapSizeInChunks.x);
        currentBiomeMinSizeInChunks = (currentBiomeMinSize / (int)mapSizeInChunks.x);
        currentBiomeMaxWidth = biomeSpawnData[i].maxBiomeSize.x;
        currentBiomeMinWidth = biomeSpawnData[i].maxBiomeSize.x;
        currentBiomeMaxLength = biomeSpawnData[i].minBiomeSize.z;
        currentBiomeMinLength = biomeSpawnData[i].minBiomeSize.z;
    }
    public List<List<GameObject>> GetSurroundingChunks(GameObject startChunk, int layersToFind) {
        var parentList = new List<List<GameObject>>();
        var layerMulti = 0f;
        for(int i = 0; i < layersToFind; i++)
        {
            var tempList = new List<GameObject>();
            Collider[] hitColliders = Physics.OverlapSphere(startChunk.transform.position, (1.25f * layerMulti));
            foreach (var hitCollider in hitColliders)
            {
                tempList.Add(hitCollider.gameObject);
            }
            parentList.Add(tempList);
            layerMulti = layerMulti + 2f;
        }
        return parentList;
    }
    #endregion
    #region Rendering Methods
    /*void CreateBiomeBatchRenderer(GameObject biomeParent, Material biomeTileMaterial) {
        BatchRendererGroup brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
        brgs.Add(brg);
        foreach (GameObject chunk in biomeParent.transform){
            GraphicsBuffer gBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)tilesPerChunk, 64);
            Matrix4x4[] transforms = new Matrix4x4[(int)tilesPerChunk];
            NativeArray<MetadataValue> batchMetaData = new NativeArray<MetadataValue>((int)tilesPerChunk * 2, Allocator.Persistent);
            BatchMeshID bMeshId = brg.RegisterMesh(tileMesh);
            BatchMaterialID bMaterialId = brg.RegisterMaterial(biomeTileMaterial);
            for (int i = 0; i < chunk.transform.childCount; i++) {
                Mesh tileMesh = chunk.transform.GetChild(i).GetComponent<MeshFilter>().sharedMesh;
                Vector3 pos = new Vector3(chunk.transform.GetChild(i).transform.position.x, chunk.transform.GetChild(i).transform.position.y, chunk.transform.GetChild(i).transform.position.z);
                transforms[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
                batchMetaData[i * 2] = new MetadataValue{NameID = Shader.PropertyToID("bMeshId"), Value = 0};
                batchMetaData[i * 2 + 1] = new MetadataValue{NameID = Shader.PropertyToID("bMaterialId"), Value = 0};
            }
            gBuffer.SetData(transforms);
            BatchID bId = brg.AddBatch(batchMetaData, gBuffer.bufferHandle);
            //BatchDrawCommand(bId);
        }
    }
    private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, ref BatchCullingContext cullingContext) {
        return default;
    }*/
    #endregion
}
