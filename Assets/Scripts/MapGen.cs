using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using Random = UnityEngine.Random;

public class MapGen : MonoBehaviour
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
    #region Map Config
    public GameObject container;
    public Vector3 mapSize;
    public Vector3 mapSizeInChunks;
    public int seed;
    private float totalChunks;
    private float totalRows;
    private float totalColumns;
    private float chunkDistanceXAxis;
    private float chunkDistanceZAxis;
    #endregion
    #region Chunk Generation
    private Vector3 chunkSize;
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
    #region Biome Data
    public List<Biome> biomes;
    public List<Biome.BiomeSpawnData> biomeSpawnData;
    private float biomeIndex;
    private Biome initialBiome;
    private Biome lastBiome;
    private float lastTemperature;
    public List<GameObject> indexedBiomes = new List<GameObject>();
    #region Current Biome Data
    private Biome currentBiome;
    private GameObject currentBiomeObject;
    private Material currentBiomeMaterial;
    private int currentBiomeMaxChunks;
    private float currentTemperature;
    private float minimumBiomeTemperature;
    private float maximumBiomeTemperature;
    private float currentBiomeMaxWidth;
    private float currentBiomeMinWidth;
    private float currentBiomeMaxLength;
    private float currentBiomeMinLength;
    #endregion
    #endregion
    #region Rendering
    private List<BatchRendererGroup> brgs;
    private Mesh tileMesh;
    private List<List<BatchID>> batchIDs;
    private List<List<BatchMaterialID>> batchMaterialIDs;
    private List<List<Mesh>> meshLists;
    private List<List<GraphicsBuffer>> transformBuffers = new List<List<GraphicsBuffer>>();
    #endregion
    #region Unity Methods & InvokeAtStart
    void Start()
    {
        Invoke(nameof(InvokeAtStart), 1);
    }
    void Update()
    {
        
    }
    void InvokeAtStart() {
        tileMesh = tilePrefab.GetComponent<MeshFilter>().sharedMesh;
        StoreBiomeRanges();
        GenerateSeed();
        tileDistanceXAxis = FindTileGapXAxis(tileSize.x);
        tileDistanceZAxis = FindTileGapZAxis(tileSize.x);
        GetMapSize();
        chunkDistanceXAxis = FindChunkGapXAxis(chunkSize.x);
        chunkDistanceZAxis = FindChunkGapZAxis(chunkSize.z);
        GetTotalChunks();
        mapSizeInChunks = new Vector3(Mathf.Round((mapSize.x / chunkSize.x)), Mathf.Round((mapSize.y / chunkSize.y)), Mathf.Round((mapSize.z / chunkSize.z)));
        initialBiome = GetInitialBiome();
        chunksToProcess += totalChunks;
    }
    #endregion
    #region Misc Methods
    void GenerateSeed() {
        seed = Random.Range(-2147483647, 2147483647);
        Random.InitState(seed);
    }
    void GetTotalChunks(){
        totalChunks = (mapSize.x / chunkSize.x) * (mapSize.z / chunkSize.z);
        tilesPerChunk = (chunkSize.x * chunkSize.z);
        rowsPerChunk = (tilesPerChunk / chunkSize.x);
    }
    void GetMapSize() {
        totalRows = mapSize.x;
        totalColumns = mapSize.z;
    }
    void GetChunkSize() {
        chunkSize = new Vector3(((
            totalRows / totalColumns) * 10f) * 0.5f
        , ((totalRows / totalColumns) * 10f) * 0.5f
        , ((totalRows / totalColumns) * 10f) * 0.5f);
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
    #region Chunk Methods
    void ProcessChunks() {
        if (chunksToProcess > 0){
            for (int i = 0; i < chunksToProcess; i++){
                chunkRow++;
                bool chunkOffset = (chunkColumn & 1) == 1;
                GenerateChunk(i, chunkOffset);
            }
        }
    }
    #endregion
    #region Chunk Utility
    void GenerateChunk(int index, bool chunkInverse) {
        #region Initalise Chunk GameObject
        GameObject chunkParent = new GameObject();
        chunkParent.AddComponent<ChunkData>();
        chunkParent.GetComponent<ChunkData>().chunkIndex = index;
        chunkParent.transform.localScale = chunkSize;
        chunkParent.transform.SetParent(currentBiomeObject.transform);
        #endregion
        #region Get Chunk Biome
        Biome chunkBiome = GetNextBiome(chunkParent);
        Material tileMat = chunkParent.GetComponent<ChunkData>().chunkBiome.biomeSpawnData.tileMaterial;
        chunkParent.GetComponent<ChunkData>().chunkBiome = chunkBiome;
        if (index == 0){
            chunkParent.GetComponent<ChunkData>().chunkBiome = initialBiome;
        }
        #endregion
        #region Place Rows
        for (int i = 0; i < rowsPerChunk; i++){
            chunkIndex++;
            if (!chunkInverse){
                offsetRow = (i & 1) == 1;
            }
            else{
                offsetRow = (i & 1) == 0;
            }
            GenerateRow(tileMat, chunkParent, offsetRow, i);
            offsetRow = false;
        }
        #endregion
    }
    void GenerateRow(Material tileMat, GameObject parent, bool rowOffset, int rowIndex) {
        for (int i = 0; i < rowsPerChunk; i++){
            float gapX = tileDistanceXAxis;
            float gapZ = tileDistanceZAxis;
            if (rowOffset){
                offset = gapX / 2f;
            }
            else{offset = 0f;}
            Vector3 tilePos = new Vector3((gapX * i) + offset , 0f, gapZ * rowIndex);
            GenerateTile(tileMat, parent, tilePos);
            offset = 0f;
        }
    }
    void GenerateTile(Material tileMat, GameObject parent, Vector3 pos){
        GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);
        //Mesh tileMesh = tile.GetComponent<MeshFilter>().sharedMesh;
        tile.GetComponent<Renderer>().material = tileMat;
        tile.transform.localScale = tileSize;
        tile.transform.eulerAngles = new Vector3(-90f,0f,0f);
        tile.transform.SetParent(parent.transform);
    }
    #endregion
    #region Biome Utility
    void AddChunkToBiomeFromIndex(GameObject parent, GameObject chunk) {
        chunk.transform.SetParent(parent.transform);
    }
    void CreateBiomeGameObject(Biome biomeType, float index) {
        GameObject biomeParent = new GameObject("Biome " + index.ToString() + "(" + biomeType.name + ")");
        currentBiomeObject = biomeParent;
        StoreBiomeData(biomeParent, biomeType);
        biomeParent.transform.SetParent(container.transform);
        AddToIndexedBiomes(biomeParent);
    }
    void StoreBiomeData(GameObject biomeParent, Biome biomeType) {
        currentBiomeMaxChunks = Mathf.RoundToInt(Random.Range(currentBiomeMaxLength, currentBiomeMinLength) + Random.Range(currentBiomeMaxWidth, currentBiomeMinWidth));
        biomeParent.AddComponent<BiomeData>();
        biomeParent.GetComponent<BiomeData>().biome = biomeType;
        biomeParent.GetComponent<BiomeData>().biomeMaterial = biomeType.biomeSpawnData.tileMaterial;
    }
    void AddToIndexedBiomes(GameObject obj) {
        indexedBiomes.Add(obj);
    }
    Biome GetBiome(float value) {
        for(int i = 0; i < biomes.Count; i++){
            var biome = biomes[i];
            if (value >= biome.biomeSpawnData.minChanceSpawn && value <= biome.biomeSpawnData.maxChanceSpawn){
                return biome;
            }
        }
        return null;
    }
    void RetrieveBiomeData(Biome biome) {
        minimumBiomeTemperature = biome.biomeSpawnData.minChanceSpawn;
        //minimumBiomeTemperature = RoundToThird(minimumBiomeTemperature);
        maximumBiomeTemperature = biome.biomeSpawnData.maxChanceSpawn;
        //maximumBiomeTemperature = RoundToThird(maximumBiomeTemperature);
        currentBiomeMaterial = biome.biomeSpawnData.tileMaterial;
        currentBiomeMaxWidth = biome.biomeSpawnData.maxBiomeSize.x;
        currentBiomeMinWidth = biome.biomeSpawnData.maxBiomeSize.x;
        currentBiomeMaxLength = biome.biomeSpawnData.minBiomeSize.z;
        currentBiomeMinLength = biome.biomeSpawnData.minBiomeSize.z;
    }
    void StoreBiomeRanges() {
        foreach (Biome biome in biomes){
            biomeSpawnData.Add(biome.biomeSpawnData);
        }
    }
    #endregion
    #region Biome Methods
    float BiomeMinMaxRandom() {
        var i = Random.Range(0f, 2.99f);
        return i;
    }
    Biome GetInitialBiome() {
        var temperature = BiomeMinMaxRandom();
        lastTemperature = temperature;
        var biome = GetBiome(temperature);
        biomeIndex = 0f;
        CreateBiomeGameObject(biome, biomeIndex);
        return biome;
    }
    Biome GetNextBiome(GameObject chunk) {
        float temperature = TemperatureCalculation();
        var biome = GetBiome(temperature);
        if (biome != lastBiome){
            biomeIndex++;
            lastBiome = biome;
            GameObject parent = indexedBiomes[(int)biomeIndex];
            CreateBiomeGameObject(biome, biomeIndex);
            RetrieveBiomeData(biome);
            return biome;
        }
        else if (biome == lastBiome){
            lastBiome = biome;
            RetrieveBiomeData(biome);
            return biome;
        }
        return null;
    }
    float TemperatureCalculation() {
        float temperature = 0f;
        float divider = Random.Range(15f, 75f);
        if (temperature > 0f) {
            temperature -= (((Random.Range(minimumBiomeTemperature, maximumBiomeTemperature) / divider) * Random.Range(currentBiomeMinLength, currentBiomeMaxLength) / divider) / 10f);
        }
        else{
            temperature = BiomeMinMaxRandom();
        }
        return temperature;
    }
    #endregion
}
