using UnityEngine;

public class UIRotation : MonoBehaviour
{
    void FixedUpdate()
    {
        transform.localRotation *= Quaternion.Euler(0, 0, 1 * Time.deltaTime * 100.0f);
    }
}
