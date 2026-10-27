using UnityEngine;

/// <summary>Poner en el prefab del pong. Informa al registro de a quién apunta.</summary>
[RequireComponent(typeof(PongProjectile))]
public class PongTargetReporter : MonoBehaviour
{
    private PongProjectile pong;
    private Transform registered;

    private void Awake() => pong = GetComponent<PongProjectile>();

    private void OnEnable()
    {
        pong.TargetChanged += OnTargetChanged;
        OnTargetChanged(pong, pong.Target); // por si ya tenía objetivo
    }

    // OnDisable cubre: Destroy, y Store() (que hace SetActive(false)).
    private void OnDisable()
    {
        pong.TargetChanged -= OnTargetChanged;
        Unregister();
    }

    private void OnTargetChanged(PongProjectile p, Transform newTarget)
    {
        Unregister();
        if (newTarget == null) return;

        // Se registra la raíz del enemigo (el target puede ser un hijo).
        var health = newTarget.GetComponentInParent<Health>();
        registered = health != null ? health.transform : newTarget;
        TargetMarkerRegistry.Add(registered);
    }

    private void Unregister()
    {
        if (registered == null) return;
        TargetMarkerRegistry.Remove(registered);
        registered = null;
    }
}