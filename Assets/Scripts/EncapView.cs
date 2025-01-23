using System;
using UnityEngine;

public class EncapView : MonoBehaviour
{
    Camera cameraComp = null;
    [SerializeField] DungeonGenerator _dungeon;

    private void Awake()
    {
        cameraComp = GetComponent<Camera>();
        _dungeon.OnDungeonGenerated += MoveCamera;
    }

    private void MoveCamera()
    {
        Bounds bounds = new Bounds();
        bounds.max = new Vector3(_dungeon.RealSize.x, 0, -_dungeon.RealSize.y);

        Vector3 extents = bounds.extents;

        float maxExtent = Mathf.Max(extents.x, extents.y, extents.z);
        float distance = maxExtent / Mathf.Tan(cameraComp.fieldOfView * 0.5f * Mathf.Deg2Rad);
        cameraComp.transform.position = bounds.center - cameraComp.transform.forward * distance;
    }
}
