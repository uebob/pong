using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Objeto pasivo: modifica estadísticas de los pongs de su dueño y/o les concede behaviors.
/// Crear con: Assets > Create > Pong > Passive Item
/// </summary>
[CreateAssetMenu(menuName = "Pong/Passive Item", fileName = "NewPassive")]
public class PassiveItem : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Tooltip("Cambios numéricos sobre las estadísticas de los pongs.")]
    public List<StatModifier> statModifiers = new List<StatModifier>();

    [Tooltip("Cambios numéricos sobre las estadísticas del JUGADOR (velocidad, saltos, dash, batjump...).")]
    public List<PlayerStatModifier> playerModifiers = new List<PlayerStatModifier>();

    [Tooltip("Comportamientos que se añaden a TODOS los pongs del dueño.")]
    public List<PongBehavior> grantedBehaviors = new List<PongBehavior>();
}