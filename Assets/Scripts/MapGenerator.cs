using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    const float XConstant = 0.01716f;
    const float ZConstant = 0.01483f;
    const float ChunkXConstant = 2.575f;
    const float ChunkZConstant = 2.225f;
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
    public GameObject chunkContainer;
    public bool isContinuous;
    public Vector3 mapSize;
    public Vector3 mapSizeInChunks;
    public int seed;
    public ChunkDetail chunkDetail;
    #endregion
    [Header("Map Data")]
    #region Map Data
    public Vector3 chunkSize;
    public float totalChunks;
    public float totalRows;
    public float totalColumns;
    private int tileCount;
    private int cIndex;
    public Biome initialBiome;
    private List<GameObject> allChunks;
    private List<GameObject> tilesUnsorted;
    private List<Renderer> tileRenderers;
    private List<GameObject> indexedBiomes;
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
    private float latestTemperature;
    public float minimumBiomeTemperature;
    public float maximumBiomeTemperature;
    #endregion
    #region Unity Methods & Un-Important
    void Start()
    {
        Invoke(nameof(InvokeAtStart), 1);
    }
    void Update()
    {
        if (chunksToProcess > 0){
            ScheduleChunkGeneration();
        }
    }
    #endregion
    #region important
    void GenerateSeed()
    {
        seed = Random.Range(-2147483647, 2147483647);
        Random.InitState(seed);
    }
    void InvokeAtStart()
    {
        StoreBiomeRanges();
        GenerateSeed();
        initialBiome = SetInitialBiome();
        tileDistanceXAxis = FindTileGapXAxis(tileSize.x);
        tileDistanceZAxis = FindTileGapZAxis(tileSize.x);
        GetMapSize();
        GetChunkDetailPercent();
        GetTotalChunks();
        mapSizeInChunks = new Vector3(Mathf.Round((mapSize.x / chunkSize.x)), Mathf.Round((mapSize.y / chunkSize.y)), Mathf.Round((mapSize.z / chunkSize.z)));
        chunksToProcess += totalChunks;
    }
    void GetMapSize(){
        totalRows = mapSize.x;
        totalColumns = mapSize.z;
    }
    void GetChunkDetailPercent(){
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
    #endregion
    #region Chunk Generation
    GameObject CreateChunkGameObject(int index) {
        GameObject chunkParent = new GameObject("Chunk " + index);
        chunkParent.AddComponent<ChunkData>();
        chunkParent.GetComponent<ChunkData>().chunkIndex = index;
        chunkParent.transform.localScale = chunkSize;
        return chunkParent;
    }
    void ScheduleChunkGeneration() {
        if (chunksToProcess > 0){
            for (int i = 0; i < chunksToProcess; i++) {
                GameObject chunkParent = CreateChunkGameObject(cIndex);
                if (cIndex == 0){
                    chunkParent.GetComponent<ChunkData>().chunkBiome = initialBiome;
                    SetChunkTileMaterial(chunkParent, initialBiome);
                }
                chunkParent.transform.SetParent(chunkContainer.transform);
                chunkRow++;
                if (Mathf.Approximately(chunkRow, mapSizeInChunks.x)){
                    chunkRow = 0;
                    chunkColumn++;
                }
                bool chunkOffset = (chunkColumn & 1) == 1;
                GenerateChunk(chunkParent, chunkOffset, cIndex);
                if (cIndex == 0){
                    chunkParent.GetComponent<ChunkData>().chunkBiome = initialBiome;
                    SetChunkTileMaterial(chunkParent, initialBiome);
                }
                chunkParent.transform.position = new Vector3(chunkSize.x + (ChunkXConstant * chunkRow), 0f, chunkSize.z + (ChunkZConstant * chunkColumn));
                cIndex++;
                chunksToProcess--;
            }
        }
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
            PlaceTile(parent, tilePos);
            offset = 0f;
        }
    }
    void PlaceTile(GameObject parent, Vector3 pos){
        GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);
        tile.transform.localScale = tileSize;
        tile.transform.eulerAngles = new Vector3(-90f,0f,0f);
        tile.transform.SetParent(parent.transform); 
    }
    #endregion
    #region Biome Utility Methods
    void AddChunkToBiomeFromIndex(int biomeIndex, GameObject chunk) {
        GameObject biome = indexedBiomes[biomeIndex];
        chunk.transform.SetParent(biome.transform);
    }
    void AddChunksToBiomeFromIndex(int biomeIndex, List<GameObject> chunks) {
        GameObject biome = indexedBiomes[biomeIndex];
        foreach (GameObject chunk in chunks){
            chunk.transform.SetParent(biome.transform);
        }
    }
    GameObject CreateBiomeGameObject(int biomeIndex) {
        GameObject biomeParent = new GameObject("Biome " + biomeIndex);
        return biomeParent;
    }
    #endregion
    #region Biome
    void StoreBiomeRanges() {
        foreach (Biome biome in biomes){
            biomeSpawnData.Add(biome.biomeSpawnData);
        }
    }
    Biome SetInitialBiome() {
        float rnd = Random.Range(-0.25f, 0.75f);
        Biome initial = biomes[0];
        for (int i = 0; i < biomeSpawnData.Count; i++) {
            var range = biomeSpawnData[i];
            if (rnd >= range.minChanceSpawn && rnd <= range.maxChanceSpawn){
                return initial;
            }
        }
        return null;
    }
    void SetChunkTileMaterial(GameObject chunk, Biome biome) {
        Material material = biome.tileMaterial;
        foreach (Transform child in chunk.transform){
            child.GetComponent<Renderer>().material = material;
        }
    }
    void NeighbourChunksBiome(int index, float latestChance, float maxChance) {
        
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
}
