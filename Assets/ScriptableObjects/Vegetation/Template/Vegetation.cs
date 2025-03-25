using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Vegetation", menuName = "Vegetation/Example")]
public class Vegetation : ScriptableObject
{
    public string vegetationName;
    
    [Header("Prefab Data")]
    public GameObject prefab;
    public Vector3 prefabOffset;
    public Vector3 prefabScale;
    public Vector3 prefabRotation;
}
