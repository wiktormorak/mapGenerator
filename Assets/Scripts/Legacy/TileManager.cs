using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChunkDetail
{
    tiny,
    small,
    medium,
    high,
}

// z is the column
// x is the row

public class TileManager : MonoBehaviour
{
    #region Prefab Configuration
    public GameObject tilePrefab;
    private Vector3 tilePrefabSize;
    public Vector3 tileSize;
    public Vector2 tilePadding;
    #endregion
    #region Configuration Variables
    public bool isContinuous;
    public Vector3 mapSize;
    public Vector3 mapSizeInChunks;
    public int seed;
    public ChunkDetail chunkDetail;
    #endregion
    #region Map Data
    public float totalChunks;
    public float totalRows;
    public float totalColumns;
    #endregion
    #region Tile Data
    private int tileCount;
    private List<GameObject> tilesUnsorted;
    private List<Renderer> tileRenderers;
    #endregion
    #region Chunk Generation
    private float sConstant = 0.01716f;
    private float tileSizeGap;
    private bool chunkInverse;
    private int chunkIndex;
    private float chunkRow;
    private float chunkColumn;
    private float chunkOffset;
    public float tilesPerChunk;
    public Vector3 chunkSize;
    public float chunksToProcess;
    #endregion
    #region Unity Methods & Un-Important
    void Start()
    {
        Invoke("InvokeAtStart", 1);
    }

    void Update()
    {
        if(chunksToProcess > 0){
            ChunkScheduler();
        }
    }
    #endregion
    #region important
    void InvokeAtStart()
    {
        tileSizeGap = (tileSize.x * sConstant);
        GetMapSize();
        GetChunkDetailPercent();
        GetTotalChunks();
        mapSizeInChunks = new Vector3((mapSize.x / chunkSize.x), (mapSize.y / chunkSize.y), (mapSize.z / chunkSize.z));
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
    }
    #endregion
    #region Chunk Generation
    /*void ChunkSchedulerOld(){
        if(chunksToProcess > 0){
            for (int i = 0; i < chunksToProcess; i++){
                chunkIndex++;
                chunkRow++;
                if(chunkRow == mapSizeInChunks.x){
                    chunkColumn++;
                    chunkRow = 0f;
                    if(chunkInverse){
                        chunkInverse = false;
                    }
                    else{
                        chunkInverse = true;
                    }
                }
                Vector3 chunkPosition = new Vector3(0f,0f,0f);
                chunkPosition = new Vector3(chunkRow * 5f, 0f, chunkColumn * 5f); //CalculateChunkPosition(chunkPosition, index);
                GameObject chunkParent = new GameObject("Chunk " + chunkIndex);
                chunkParent.transform.localScale = chunkSize;
                CreateChunk(chunkParent, chunkIndex);
                chunkParent.transform.localPosition = chunkPosition;
                chunksToProcess--;
            }
        }
    }*/
    void ChunkScheduler(){
        if (chunksToProcess > 0){
            for (int i = 0; i < chunksToProcess; i++){
                chunkIndex++;
                chunkRow++;
                if (chunkIndex == mapSizeInChunks.x){
                    chunkColumn++;
                    chunkRow = 0f;
                    chunkInverse = !chunkInverse;
                }
                var chunkPosition = new Vector3(chunkRow * chunkSize.x, 0f, chunkColumn * chunkSize.z);
                CreateChunk(CreateChunkGameObject(chunkPosition, chunkIndex), chunkIndex);
                chunksToProcess--;
            }
        }
    }
    GameObject CreateChunkGameObject(Vector3 chunkPosition, int index) {
        GameObject chunkParent = new GameObject("Chunk " + index);
        chunkParent.transform.localScale = chunkSize;
        chunkParent.transform.position = chunkPosition;
        return chunkParent;
    }
    void CreateChunk(GameObject parent, int index){
        float tileRow = 0f;
        float tileColumn = 0f;
        float tileColumnOffset = 0f;
        float tileRowOffset = 0f;
        Vector3 currentPos = new Vector3(0f,0f,0f);
        float rowMax = 5f;
        for(int i = 0; i < tilesPerChunk; i++){
            tileColumn++;
            if(i == rowMax){
                rowMax = rowMax + 5f;
                tileRow++;
                tileColumn = 0f;
            }
            if(tileRow != 0){
                if(!chunkInverse){
                    if(tileRow == 1 || tileRow == 3){
                        tileColumnOffset += 0.5f;
                        //tileRowOffset -= 0.2f;
                    }
                    else{
                        tileColumnOffset += 1f;
                    }
                }
            }
            if(chunkInverse){
                if(tileRow == 0 || tileRow == 2 || tileRow == 4){
                    tileColumnOffset += 0.5f;
                }
                else{
                    tileColumnOffset += 1f;
                    //tileRowOffset -= 0.2f;
                }
            }
            currentPos = new Vector3(tileColumn + tileColumnOffset, 0f, tileRow);
            PlaceTile(parent, currentPos);
            tileColumnOffset = 0f;
        }
    }
    void PlaceTile(GameObject parent, Vector3 pos){
        GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity);
        tile.transform.localScale = tileSize;
        tile.transform.eulerAngles = new Vector3(-90f,0f,0f);
        tile.transform.SetParent(parent.transform); 
    }
    #endregion
}
