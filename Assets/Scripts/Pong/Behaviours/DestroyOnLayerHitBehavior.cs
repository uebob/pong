using UnityEngine;

/// <summary>
/// Destruye el pong al colisionar con objetos de ciertas capas (Ground, Wall...).
/// </summary>
[CreateAssetMenu(menuName = "Pong/Behaviors/Destroy On Layer Hit", fileName = "DestroyOnLayerHit")]
public class DestroyOnLayerHitBehavior : PongBehavior
{
    [Tooltip("Capas que destruyen el pong al golpearlas.")]
    [SerializeField] private LayerMask destroyLayers;

    public override void OnHit(PongProjectile pong, Collision collision)
    {
        int layer = collision.gameObject.layer;
        if ((destroyLayers.value & (1 << layer)) == 0) return;

        pong.Despawn();
    }
}