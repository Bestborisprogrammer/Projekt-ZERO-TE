using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = Vector3.zero;
    public float smoothSpeed = 0f; // set to 0 for instant follow, higher for smooth

    void LateUpdate()
    {
        if (!target) return;

        Vector3 targetPos = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            -10f);

        if (smoothSpeed <= 0f)
            transform.position = targetPos;
        else
            transform.position = Vector3.Lerp(
                transform.position, targetPos, smoothSpeed * Time.deltaTime);
    }
}