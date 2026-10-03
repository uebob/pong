using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un TIPO de pong: prefab, feel, estadísticas base y behaviors innatos.
/// Cada tipo es un asset duplicable. Crear con: Assets > Create > Pong > Pong Definition
///
/// Importante: la definición que cuenta es la que se pasa a Spawn() (p. ej. la de
/// PongInventory.startingPongs). El campo "definition" del propio prefab solo se usa
/// para pongs colocados a mano en escena.
/// </summary>
[CreateAssetMenu(menuName = "Pong/Pong Definition", fileName = "NewPong")]
public class PongDefinition : ScriptableObject
{
    [Header("Identidad")]
    public string displayName;
    public Sprite icon;
    [Tooltip("Se aplica a los renderers del pong al crearlo (útil para distinguir tipos).")]
    public Color color = Color.white;

    [Header("Prefab y feel")]
    public PongProjectile prefab;
    [Tooltip("OBLIGATORIO: curvas y tiempos del movimiento. Sin esto el pong usa valores por defecto.")]
    public ProjectileSettings settings;

    [Header("Estadísticas base")]
    [Tooltip("Las que no aparezcan aquí valen 0 (un pong con CruiseSpeed 0 no se mueve). " +
             "Si la lista está vacía: menú ⋮ del componente > Restore Default Stats.")]
    public List<StatValue> baseStats = DefaultStats();

    [Header("Comportamientos propios")]
    public List<PongBehavior> innateBehaviors = new List<PongBehavior>();

    private static List<StatValue> DefaultStats() => new List<StatValue>
    {
        new StatValue { stat = PongStat.CruiseSpeed,          value = 12f },
        new StatValue { stat = PongStat.MaxCruiseSpeed,       value = 30f },
        new StatValue { stat = PongStat.SpeedGainPerParry,    value = 2f },
        new StatValue { stat = PongStat.ParryBoostMultiplier, value = 2f },
        new StatValue { stat = PongStat.TurnRate,             value = 200f },
        new StatValue { stat = PongStat.Damage,               value = 0f },
        new StatValue { stat = PongStat.SpeedDamageScale,     value = 1f },
    };

    [ContextMenu("Restore Default Stats")]
    private void RestoreDefaultStats()
    {
        baseStats = DefaultStats();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void OnValidate()
    {
        if (settings == null)
            Debug.LogWarning($"PongDefinition '{name}': falta asignar ProjectileSettings.", this);
    }

    /// <summary>
    /// Instancia un pong de este tipo. owner y faction se fijan para siempre.
    /// Después, guárdalo en un inventario (PongInventory.TryStore) o lánzalo
    /// (PongProjectile.Launch). Ejemplo enemigo:
    /// definition.Spawn(gameObject, PongFaction.Enemy, pos, rot).Launch(pos, dir);
    /// </summary>
    public PongProjectile Spawn(GameObject owner, PongFaction faction, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError($"PongDefinition '{name}' no tiene prefab.", this);
            return null;
        }

        PongProjectile pong = Instantiate(prefab, position, rotation);
        pong.Setup(this, owner, faction);
        return pong;
    }
}