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
    public Material tileMaterial;
    
    [System.Serializable]
    public struct BiomeSpawnData
    {
        public string biomeName;
        public float minChanceSpawn;
        public float maxChanceSpawn;
        public float minHeight;
        public float maxHeight;
        public int maxBiomeSize;
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

