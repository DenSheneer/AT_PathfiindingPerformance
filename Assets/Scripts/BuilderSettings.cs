using System;
using UnityEngine;

[Serializable]

[CreateAssetMenu(fileName = "BuilderSettings", menuName = "Custom/Create Builder Settings")]
public class BuilderSettings : ScriptableObject
{
    [SerializeField] public GameObject[] WallPrefabs;
    [SerializeField] public GameObject[] FloorPrefabs;
    [SerializeField] public GameObject[] DoorPrefabs;
    [SerializeField] public GameObject WallCornerPrefab;
    [SerializeField] public float offsetBetweenAssets;
    [SerializeField] public float offsetWall;
}
