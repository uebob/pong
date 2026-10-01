using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un TIPO de pong: prefab, feel, estadísticas base y behaviors innatos.
/// Cada tipo es un asset duplicable. Crear con: Assets > Create > Pong > Pong Definition
/// </summary>
[CreateAssetMenu(menuName = "Pong/Pong Definition", fileName = "NewPong")]
public class PongDefinition : ScriptableObject
{
    [Header("Identidad")]
    public string displayName;
    public Sprite icon;
    public Color color = Color.white;

    [Header("Prefab y feel")]
    public PongProjectile prefab;
    public ProjectileSettings settings;

    [Header("Estadísticas base")]
    [Tooltip("Las que no aparezcan aquí valen 0.")]
    public List<StatValue> baseStats = new List<StatValue>
    {
        new StatValue { stat = PongStat.CruiseSpeed,          value = 12f },
        new StatValue { stat = PongStat.MaxCruiseSpeed,       value = 30f },
        new StatValue { stat = PongStat.SpeedGainPerParry,    value = 2f },
        new StatValue { stat = PongStat.ParryBoostMultiplier, value = 2f },
        new StatValue { stat = PongStat.TurnRate,             value = 200f },
        new StatValue { stat = PongStat.Damage,               value = 10f },
    };

    [Header("Comportamientos propios")]
    public List<PongBehavior> innateBehaviors = new List<PongBehavior>();

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