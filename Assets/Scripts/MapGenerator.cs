using System;
using System.Collections.Generic;
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
    public int currentBiomeMaxSize;
    private float biomeSpawnRandom;
    #endregion
    #region Rendering
    private List<BatchRendererGroup> brg;
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
        initialBiome = SetInitialBiome();
        tileDistanceXAxis = FindTileGapXAxis(tileSize.x);
        tileDistanceZAxis = FindTileGapZAxis(tileSize.x);
        GetMapSize();
        GetChunkDetailPercent();
        chunkDistanceXAxis = FindChunkGapXAxis(chunkSize.x);
        chunkDistanceZAxis = FindChunkGapZAxis(chunkSize.z);
        GetTotalChunks();
        mapSizeInChunks = new Vector3(Mathf.Round((mapSize.x / chunkSize.x)), Mathf.Round((mapSize.y / chunkSize.y)), Mathf.Round((mapSize.z / chunkSize.z)));
        chunksToProcess += totalChunks;
    }
    #endregion
    #region important Methods
    void GenerateSeed()
    {
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
        GameObject chunkParent = new GameObject("Chunk " + index);
        chunkParent.AddComponent<ChunkData>();
        chunkParent.GetComponent<ChunkData>().chunkIndex = index;
        chunkParent.transform.localScale = chunkSize;
        return chunkParent;
    }
    void ChunkScheduler() {
        if (chunksToProcess > 0){
            /*BatchID batchId = new BatchID();
            List<BatchID> batchIdList = new List<BatchID>();
            List<Mesh> meshList = new List<Mesh>();
            meshLists.Add(meshList);
            batchIDs.Add(batchIdList)*/;
            for (int i = 0; i < chunksToProcess; i++) {
                GameObject chunkParent = CreateChunkGameObject(cIndex);
                #region First Chunk
                if (cIndex == 0){
                    chunkParent.GetComponent<ChunkData>().chunkBiome = initialBiome;
                    SetChunkTileMaterial(chunkParent);
                }
                #endregion
                Biome chunkBiome = GetNextBiome(chunkParent);
                chunkParent.name = lastTemperature.ToString();
                SetChunkBiome(chunkParent,  chunkBiome);
                chunkRow++;
                #region Change Column
                if (Mathf.Approximately(chunkRow, mapSizeInChunks.x)){
                    chunkRow = 0;
                    chunkColumn++;
                }
                #endregion
                bool chunkOffset = (chunkColumn & 1) == 1;
                GenerateChunk(chunkParent, chunkOffset, cIndex);
                chunkParent.transform.position = SetChunkPosition(chunkRow, chunkColumn);
                SetChunkTileMaterial(chunkParent);
                cIndex++;
                chunksToProcess--;
            }
        }
    }
    Vector3 SetChunkPosition(int row, int column) {
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
        Mesh tileMesh = tile.GetComponent<MeshFilter>().sharedMesh;
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
        GameObject biomeParent = new GameObject("Biome " + index.ToString() + "(" + biomeType.ToString() + ")");
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
    Biome SetInitialBiome()
    {
        biomeSpawnRandom = BiomeMinMaxRandom();
        biomeSpawnRandom = Mathf.Floor(biomeSpawnRandom * 1000) / 1000;
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
        float divider = Random.Range(5f, 150f);
        if (lastTemperature > 0f){
            lastTemperature -= (((Random.Range(minimumBiomeTemperature, maximumBiomeTemperature) * divider) / (currentBiomeMaxSize * mapSizeInChunks.x) * divider) / 100f);
        }
        else{
            lastTemperature = BiomeMinMaxRandom();
        }
        lastTemperature = Mathf.Floor(lastTemperature * 1000) / 1000;
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
        minimumBiomeTemperature = Mathf.Floor(minimumBiomeTemperature * 1000) / 1000;
        maximumBiomeTemperature = biomeSpawnData[i].maxChanceSpawn;
        maximumBiomeTemperature = Mathf.Floor(maximumBiomeTemperature * 1000) / 1000;
        currentBiomeMaterial = biomeSpawnData[i].tileMaterial;
        currentBiomeMaxSize = biomeSpawnData[i].maxBiomeSize;
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
    void CreateBiomeBatchRenderer(GameObject biomeParent) {
        //BatchRendererGroup brg = new BatchRendererGroup(BatchRendererGroup.OnPerformCulling(), IntPtr.Zero);
    }
    #endregion
}
