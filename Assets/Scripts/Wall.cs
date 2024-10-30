using UnityEngine;

public class Wall : MonoBehaviour
{
    private GameObject[] _segments;
    BuilderSettings _settings;
    public void Initialize(GameObject[] segments, BuilderSettings settings)
    {
        _settings = settings;
        _segments = segments;

        foreach (GameObject segment in _segments)
        {
            segment.transform.parent = transform;
        }
    }

    public void MakeDoorInMiddle()
    {
        var middle = _segments[Mathf.FloorToInt(_segments.Length * 0.5f)];
        var door = Instantiate(_settings.DoorPrefabs[0]);
        door.transform.parent = transform;
        door.transform.SetLocalPositionAndRotation(middle.transform.localPosition, middle.transform.localRotation);
        Destroy(_segments[Mathf.FloorToInt(_segments.Length * 0.5f)]);
        _segments[Mathf.FloorToInt(_segments.Length * 0.5f)] = door;
    }
}
