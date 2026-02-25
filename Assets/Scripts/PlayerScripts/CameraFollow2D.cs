using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;

    void LateUpdate()
    {
        if (!target) return;

        Vector3 newPosition = target.position + offset;
        newPosition.z = -10f; // keep camera in front

        transform.position = newPosition;
    }
}