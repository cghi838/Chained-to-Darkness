using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool useRaycastInteraction = true;
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private LayerMask interactableLayers;

    [Header("References")]
    [SerializeField] private Transform rayOrigin;

    private InteractableObject currentTriggerInteractable;

    private void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (useRaycastInteraction)
                TryRaycastInteraction();
            else
                TryTriggerInteraction();
        }
    }

    private void TryRaycastInteraction()
    {
        Transform origin = rayOrigin != null ? rayOrigin : transform;

        RaycastHit2D hit = Physics2D.Raycast(
            origin.position,
            transform.right,
            interactRange,
            interactableLayers
        );

        if (hit.collider == null)
            return;

        InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();

        if (interactable != null)
            interactable.Interact();
    }

    private void TryTriggerInteraction()
    {
        if (currentTriggerInteractable != null)
            currentTriggerInteractable.Interact();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        InteractableObject interactable = other.GetComponent<InteractableObject>();

        if (interactable != null)
            currentTriggerInteractable = interactable;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        InteractableObject interactable = other.GetComponent<InteractableObject>();

        if (interactable != null && currentTriggerInteractable == interactable)
            currentTriggerInteractable = null;
    }
}