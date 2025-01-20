using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.HableCurve;

public class Floor : MonoBehaviour
{
    GameObject[] _tiles = null;
    private List<MeshRenderer> _floorMeshes;
    private Material _defaultFloorMaterial;

    public List<MeshRenderer> FloorMeshes { set { _floorMeshes = value; _defaultFloorMaterial = _floorMeshes[0].material; } }

    public void Initilize(GameObject[] tiles)
    {
        _tiles = tiles;
    }

    public GameObject GetMiddleTile()
    {
        var middle = _tiles[Mathf.FloorToInt(_tiles.Length * 0.5f)];
        return middle;
    }
    public void SetPathMaterial(Material material)
    {
        foreach (var tile in _floorMeshes)
        {
            tile.material = material;
        }
    }

    public void RestoreDefaultFloor()
    {        
        SetPathMaterial(_defaultFloorMaterial);
    }
}
