using UnityEngine;

public class FogZoneTrigger : MonoBehaviour
{
    public FogEffect fogEffect;
    public bool activateOnEnter = true; // true = enter zone, false = exit zone

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        fogEffect?.SetActive(activateOnEnter);
    }
}