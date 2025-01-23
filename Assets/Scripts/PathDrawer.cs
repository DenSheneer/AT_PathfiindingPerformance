using System.Collections.Generic;
using UnityEngine;

public class PathDrawer : MonoBehaviour
{
    [SerializeField] private bool _doDraw = true;

    [Header("Materials")]
    [SerializeField] private Material _startPositionMaterial;
    [SerializeField] private Material _goalMaterial;
    [SerializeField] private Material _multiMaterial;
    [SerializeField] private Material _DFS_Material;
    [SerializeField] private Material _asyncMaterial;
    [SerializeField] private Material _aStarMaterial;

    private void Start()
    {
        foreach (var agent in FindObjectsByType<PathfindAgent>(FindObjectsSortMode.None))
        {
            agent.OnPathFound += drawPath;
        }
    }
    private void drawPath(List<Room> path, EPathFindMode mode)
    {
        if (!_doDraw) { return; }

        if (path != null)
        {
            var currentMaterial = determineMaterial(mode);

            foreach (var room in path)
            {
                room.SetFloorMaterial(currentMaterial);
            }
            path[0].SetFloorMaterial(_startPositionMaterial);
            path[path.Count - 1].SetFloorMaterial(_goalMaterial);
        }
    }

    private Material determineMaterial(EPathFindMode mode)
    {
        switch (mode)
        {
            case EPathFindMode.DFS:
                return _DFS_Material;
            case EPathFindMode.AsyncDFS:
                return _asyncMaterial;
            case EPathFindMode.MT_DFS:
                return _multiMaterial;
            case EPathFindMode.Astar:
                return _aStarMaterial;

        }
        return _DFS_Material;
    }
}