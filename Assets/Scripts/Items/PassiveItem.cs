using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Objeto pasivo: modifica estadísticas de los pongs de su dueño y/o del jugador, les concede
/// behaviors y/o ejecuta efectos (PassiveEffect) al recogerlo o al hacer acciones.
/// Crear con: Assets > Create > Pong > Passive Item
/// </summary>
[CreateAssetMenu(menuName = "Pong/Passive Item", fileName = "NewPassive")]
public class PassiveItem : ScriptableObject
{
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Tooltip("Modelo 3D pequeño que se muestra cuando el item está en el suelo (lo usa PassivePickup). " +
             "Opcional: si lo dejas vacío, el pickup debe traer su propio modelo.")]
    public GameObject worldModel;

    [Tooltip("Si es false, solo puedes tener una copia: si ya lo tienes, el objeto del suelo no se recoge. " +
             "Si es true, cada copia suma sus efectos.")]
    public bool stackable = true;

    [Tooltip("Si es true, el objeto se CONSUME al recogerlo: ejecuta sus efectos (OnAcquired) y no se guarda, " +
             "así que no aparece en la lista de pasivos. Ideal para curas y recompensas instantáneas.")]
    public bool consumable = false;

    [Tooltip("Cambios numéricos sobre las estadísticas de los pongs.")]
    public List<StatModifier> pongStatModifiers = new List<StatModifier>();

    [Tooltip("Cambios numéricos sobre las estadísticas del JUGADOR (velocidad, saltos, dash, batjump...).")]
    public List<PlayerStatModifier> playerModifiers = new List<PlayerStatModifier>();

    [Tooltip("Comportamientos que se añaden a TODOS los pongs del dueño.")]
    public List<PongBehavior> grantedBehaviors = new List<PongBehavior>();

    [Tooltip("Efectos con lógica propia: curar al recoger, reaccionar a un parry, un dash, recibir daño...")]
    public List<PassiveEffect> effects = new List<PassiveEffect>();
}