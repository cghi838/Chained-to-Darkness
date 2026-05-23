using System.Collections;
using UnityEngine;

// RewardZone — Spawns a reward object when player enters.
// Place a trigger collider where you want the reward event to happen.
// Set playerLayer in Inspector to the "Player" layer.
[RequireComponent(typeof(Collider2D))]
public class RewardZone : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask playerLayer;

    [Header("Reward Fragment")]
    [Tooltip("Reward fragment prefab to spawn when player enters.")]
    public GameObject fragmentPrefab;
    public Vector3 spawnOffset = new Vector3(0f, 3f, 0f);
    public float spawnDelay = 1f;

    [Header("Options")]
    public bool spawnOnlyOnce = true;

    private bool hasSpawned = false;
    private bool isSpawning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[RewardZone] Trigger entered by: {other.gameObject.name} (layer: {LayerMask.LayerToName(other.gameObject.layer)})");
        
        if (((1 << other.gameObject.layer) & playerLayer) == 0)
        {
            Debug.Log("[RewardZone] Layer mismatch — not player layer");
            return;
        }

        if (fragmentPrefab == null)
        {
            Debug.LogWarning("[RewardZone] fragmentPrefab is not assigned.");
            return;
        }

        if (spawnOnlyOnce && hasSpawned) return;
        if (isSpawning) return;

        StartCoroutine(SpawnRewardRoutine());
    }

    private IEnumerator SpawnRewardRoutine()
    {
        isSpawning = true;

        yield return new WaitForSeconds(spawnDelay);

        Instantiate(fragmentPrefab, transform.position + spawnOffset, Quaternion.identity);

        hasSpawned = true;
        isSpawning = false;
    }
}
