using TMPro;
using UnityEngine;

public class PopupText : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private float moveSpeed = 0.5f;
    [SerializeField] private float lifetime = 2f;

    private float timer;

    private void Awake()
    {
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshPro>();
    }

    public void Setup(string message, float customLifetime)
    {
        if (textMesh != null)
            textMesh.text = message;
        lifetime = customLifetime;
    }

    private void Update()
    {
        transform.localPosition += Vector3.up * moveSpeed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifetime)
            Destroy(gameObject);
    }
}
