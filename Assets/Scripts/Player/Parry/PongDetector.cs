using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detecta pongs cerca del jugador. Lo comparten el parry y el guardado (E),
/// así "a distancia de parry" significa lo mismo para los dos.
/// </summary>
public class PongDetector : MonoBehaviour
{
    [SerializeField] private float radius = 3f;
    [SerializeField] private LayerMask projectileLayer;

    public float Radius => radius;

    private readonly Collider[] buffer = new Collider[32];

    /// <summary>Rellena results con los pongs en rango, del más cercano al más lejano.</summary>
    public int Query(List<PongProjectile> results)
    {
        results.Clear();
        Vector3 origin = transform.position;

        int count = Physics.OverlapSphereNonAlloc(
            origin, radius, buffer, projectileLayer, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            var pong = buffer[i].GetComponentInParent<PongProjectile>();
            if (pong != null && !results.Contains(pong))
                results.Add(pong);
        }

        results.Sort((a, b) =>
            (a.transform.position - origin).sqrMagnitude
                .CompareTo((b.transform.position - origin).sqrMagnitude));

        return results.Count;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
