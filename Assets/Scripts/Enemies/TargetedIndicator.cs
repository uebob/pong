using UnityEngine;

/// <summary>Poner en el enemigo. Muestra un reborde rojo mientras algún pong lo apunta.</summary>
public class TargetedIndicator : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;      // el MeshRenderer de la cápsula
    [SerializeField] private Material outlineMaterial;     // material del reborde

    private Transform root;
    private Material[] original;
    private Material[] withOutline;
    private bool active;

    private void Awake()
    {
        var health = GetComponentInParent<Health>();
        root = health != null ? health.transform : transform;
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();

        original = targetRenderer.sharedMaterials;
        withOutline = new Material[original.Length + 1];
        original.CopyTo(withOutline, 0);
        withOutline[withOutline.Length - 1] = outlineMaterial;
    }

    private void OnEnable()
    {
        TargetMarkerRegistry.CountChanged += OnCountChanged;
        SetActive(TargetMarkerRegistry.GetCount(root) > 0);
    }

    private void OnDisable()
    {
        TargetMarkerRegistry.CountChanged -= OnCountChanged;
        SetActive(false);
    }

    private void OnCountChanged(Transform changedRoot, int count)
    {
        if (changedRoot == root) SetActive(count > 0);
    }

    private void SetActive(bool value)
    {
        if (active == value) return;
        active = value;
        targetRenderer.sharedMaterials = value ? withOutline : original;
    }
}