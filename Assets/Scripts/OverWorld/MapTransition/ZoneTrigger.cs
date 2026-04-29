using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    [Header("Destination")]
    public Transform destinationPoint;  // drag the exit point here
    public Vector3 manualDestination;   // or set a manual position
    public bool useManualDestination = false;

    private bool isTransitioning = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isTransitioning) return;
        if (!other.CompareTag("Player")) return;

        isTransitioning = true;

        Vector3 target = useManualDestination
            ? manualDestination
            : destinationPoint != null
                ? destinationPoint.position
                : transform.position;

        FadeTransition.Instance.FadeToPosition(target, () =>
        {
            Debug.Log($"Teleported to {target}");
        });

        // Reset after transition
        Invoke(nameof(ResetTransition), 2f);
    }

    void ResetTransition()
    {
        isTransitioning = false;
    }
}