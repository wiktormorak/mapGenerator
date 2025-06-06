using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeExample", menuName = "Biome/Example")]
public class Biome : ScriptableObject
{
    [Header("Debug")]
    public Color biomeDebugColor;
    
    [Header("Basic Data")]
    public string biomeName;
    public int rarityPercent;
    public BiomeSpawnData biomeSpawnData;
    
    [System.Serializable]
    public struct BiomeSpawnData
    {
        public string biomeName;
        public float minChanceSpawn;
        public float maxChanceSpawn;
        public Vector3 maxBiomeSize;
        public Vector3 minBiomeSize;
        //public int minBiomeSize; replaced with Vector3 parameters with the same name (now set in total chunks)
        //public int maxBiomeSize; replaced with Vector3 parameters with the same name (now set in total chunks)
        public Material tileMaterial;
    }

    [Header("Land Configuration")]
    public bool allowsLand;
    public bool allowsMountains;
    public bool allowsIslands;
    public bool allowsContinental;
    public bool allowsSandyBeaches;
    public bool allowsRockyBeaches;
    
    [Header("Water Configuration")]
    public bool allowsVegetation;
    public bool allowsWaterBodies;
    public bool allowsPonds;
    public bool allowsRivers;
    public bool allowsLakes;
    public bool allowsOceans;
    
    [Header("Vegetation Configuration")]
    public float treeDensityPercent;
    public List<Vegetation> vegetation;
    
    [Header("Weather Data")]
    public float temperature;
    public float humidity;
    public float rainfall;
    public float windSpeed;
    
}

