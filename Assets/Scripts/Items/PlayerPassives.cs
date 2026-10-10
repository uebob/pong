using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pasivos activos del jugador. Los enemigos pueden implementar IPongModifierProvider
/// igual para tener sus propios pasivos.
/// Los pongs resuelven sus estadísticas al lanzarse (y al cambiar de dueño), así que un
/// pasivo recogido afecta a todos los pongs guardados en el siguiente lanzamiento.
///
/// Además es la CENTRALITA de los PassiveEffect: se suscribe a los eventos del jugador
/// (parry, daño, batjump, salto, dash, ground pound) y los reenvía a los efectos de los objetos que tienes.
/// </summary>
public class PlayerPassives : MonoBehaviour, IPongModifierProvider
{
    [SerializeField] private List<PassiveItem> items = new List<PassiveItem>();

    public IReadOnlyList<PassiveItem> ActivePassives => items;
    public event Action Changed;

    private PlayerContext context;
    private PlayerParry parry;
    private PlayerMovement movement;
    private Health health;

    // Copia para recorrer: un efecto podría añadir o quitar objetos mientras se despacha
    private readonly List<PassiveItem> scratch = new List<PassiveItem>();

    private void Awake()
    {
        parry = GetComponentInChildren<PlayerParry>(true);
        movement = GetComponentInChildren<PlayerMovement>(true);
        health = GetComponentInChildren<Health>(true);

        context = new PlayerContext
        {
            Player = gameObject,
            Passives = this,
            Health = health,
            Movement = movement,
            Inventory = GetComponentInChildren<PongInventory>(true),
            Wallet = GetComponentInChildren<Wallet>(true),
        };
    }

    private void OnEnable()
    {
        if (parry != null) parry.ParrySucceeded += HandleParry;
        if (health != null) health.Damaged += HandleDamaged;
        if (movement != null)
        {
            movement.BatJumped += HandleBatJump;
            movement.Jumped += HandleJump;
            movement.Dashed += HandleDash;
            movement.GroundPoundLanded += HandleGroundPoundLand;
        }
    }

    private void OnDisable()
    {
        if (parry != null) parry.ParrySucceeded -= HandleParry;
        if (health != null) health.Damaged -= HandleDamaged;
        if (movement != null)
        {
            movement.BatJumped -= HandleBatJump;
            movement.Jumped -= HandleJump;
            movement.Dashed -= HandleDash;
            movement.GroundPoundLanded -= HandleGroundPoundLand;
        }
    }

    private void Start()
    {
        // Objetos con los que empieza el jugador (puestos en el inspector): se ejecuta su OnAcquired
        scratch.Clear();
        scratch.AddRange(items);
        foreach (PassiveItem item in scratch)
            RunEffects(item, e => e.OnAcquired(context));
    }

    // ------------------------------------------------------------------
    // Estadísticas
    // ------------------------------------------------------------------

    /// <summary>
    /// Aplica los pasivos activos a un valor base del jugador: (base + suma de Add) × producto de Multiply.
    /// Se calcula en cada llamada (las listas son pequeñas), así que cualquier cambio en la lista,
    /// incluso editándola en el inspector en juego, se refleja al instante.
    /// </summary>
    public float Apply(PlayerStat stat, float baseValue)
    {
        float add = 0f;
        float mul = 1f;

        for (int i = 0; i < items.Count; i++)
        {
            PassiveItem item = items[i];
            if (item == null || item.playerModifiers == null) continue;

            List<PlayerStatModifier> modifiers = item.playerModifiers;
            for (int j = 0; j < modifiers.Count; j++)
            {
                if (modifiers[j].stat != stat) continue;

                if (modifiers[j].op == ModifierOp.Add) add += modifiers[j].value;
                else mul *= modifiers[j].value;
            }
        }

        return (baseValue + add) * mul;
    }

    /// <summary>Igual que Apply, para estadísticas enteras (resultado redondeado).</summary>
    public int ApplyInt(PlayerStat stat, int baseValue) =>
        Mathf.RoundToInt(Apply(stat, baseValue));

    // ------------------------------------------------------------------
    // Añadir / quitar
    // ------------------------------------------------------------------

    /// <summary>
    /// Añade el item. Devuelve false si no se pudo (item nulo, no apilable y ya lo tienes,
    /// o algún efecto lo rechaza con CanAcquire). Un item consumible ejecuta sus efectos y NO se guarda.
    /// </summary>
    public bool Add(PassiveItem item)
    {
        if (item == null) return false;
        if (!item.stackable && items.Contains(item)) return false;
        if (!CanAcquire(item)) return false;

        if (!item.consumable) items.Add(item);

        RunEffects(item, e => e.OnAcquired(context));

        if (!item.consumable) Changed?.Invoke();
        return true;
    }

    public bool Remove(PassiveItem item)
    {
        if (!items.Remove(item)) return false;

        RunEffects(item, e => e.OnRemoved(context));

        // El estado de un efecto se descarta cuando ya no queda ningún objeto que lo use
        if (item != null && item.effects != null)
            foreach (PassiveEffect effect in item.effects)
                if (effect != null && !IsEffectActive(effect))
                    context.ClearState(effect);

        Changed?.Invoke();
        return true;
    }

    private bool CanAcquire(PassiveItem item)
    {
        if (item.effects == null) return true;

        foreach (PassiveEffect effect in item.effects)
            if (effect != null && !effect.CanAcquire(context))
                return false;

        return true;
    }

    private bool IsEffectActive(PassiveEffect effect)
    {
        foreach (PassiveItem item in items)
            if (item != null && item.effects != null && item.effects.Contains(effect))
                return true;

        return false;
    }

    // ------------------------------------------------------------------
    // Despacho de efectos
    // ------------------------------------------------------------------

    private void RunEffects(PassiveItem item, Action<PassiveEffect> call)
    {
        if (item == null || item.effects == null) return;

        foreach (PassiveEffect effect in item.effects)
            if (effect != null) call(effect);
    }

    /// <summary>Ejecuta 'call' en los efectos de todos los objetos que tienes.</summary>
    private void Dispatch(Action<PassiveEffect> call)
    {
        scratch.Clear();
        scratch.AddRange(items);

        for (int i = 0; i < scratch.Count; i++)
            RunEffects(scratch[i], call);
    }

    private void HandleParry(PongProjectile pong) => Dispatch(e => e.OnParry(context, pong));
    private void HandleDamaged(float amount) => Dispatch(e => e.OnDamaged(context, amount));
    private void HandleBatJump(Vector3 point, Vector3 normal) => Dispatch(e => e.OnBatJump(context, point, normal));
    private void HandleJump() => Dispatch(e => e.OnJump(context));
    private void HandleDash() => Dispatch(e => e.OnDash(context));
    private void HandleGroundPoundLand() => Dispatch(e => e.OnGroundPoundLand(context));
}