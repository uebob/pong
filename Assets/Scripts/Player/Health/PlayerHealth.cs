using System.Collections;
using UnityEngine;

/// <summary>
/// Vida del jugador. Recibe daño de cualquier pong, también de los suyos.
/// El daño de un pong se aplica tras un periodo de gracia: si en ese tiempo el pong se parrea
/// (o se guarda), no hay daño. Da margen para parrear "justo tarde".
/// Tras recibir el golpe, el pong se intenta guardar en la cola.
/// </summary>
public class PlayerHealth : Health
{
    [Tooltip("Tiempo (en tiempo real) tras el golpe durante el cual un parry aún cancela el daño. 0 = daño inmediato.")]
    [SerializeField] private float collisionGracePeriod = 0.1f;

    private PongInventory inventory;

    protected override void Start()
    {
        base.Start();
        inventory = GetComponent<PongInventory>();
    }

    public override void TakeDamage(float amount, PongProjectile source)
    {
        // Sin pong de por medio, o sin gracia: inmediato.
        if (source == null || collisionGracePeriod <= 0f)
        {
            Resolve(amount, source);
            return;
        }

        StartCoroutine(ResolveAfterGrace(amount, source));
    }

    private IEnumerator ResolveAfterGrace(float amount, PongProjectile source)
    {
        // Un parry incrementa ParryCount: comparar con el valor del golpe evita referencias caducadas.
        int parriesAtHit = source.ParryCount;

        yield return new WaitForSecondsRealtime(collisionGracePeriod);

        // Si el pong sigue existiendo, comprobamos que no lo hayan parreado ni guardado durante la gracia.
        // Si ya no existe (p. ej. explotó al golpearte), el daño era inevitable: se aplica.
        if (source != null)
        {
            if (source.ParryCount != parriesAtHit) yield break;                     // parreado durante la gracia
            if (source.CurrentState == PongProjectile.State.InHolster) yield break; // ya guardado
        }

        Resolve(amount, source);
    }

    private void Resolve(float amount, PongProjectile source)
    {
        ApplyDamage(amount);

        if (!IsDead && source != null && inventory != null)
            inventory.TryStore(source);
    }
}