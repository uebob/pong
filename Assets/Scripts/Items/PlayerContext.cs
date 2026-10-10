using System.Collections.Generic;

/// <summary>
/// Referencias al jugador que reciben los PassiveEffect, para que no tengan que buscarlas.
/// Lo crea PlayerPassives. Cualquier campo puede ser null si el jugador no tiene ese componente.
/// Para dar acceso a algo nuevo a los efectos, añade aquí el campo y rellénalo en PlayerPassives.Awake.
/// </summary>
public class PlayerContext
{
    public UnityEngine.GameObject Player;
    public PlayerPassives Passives;
    public Health Health;
    public PlayerMovement Movement;
    public PongInventory Inventory;
    public Wallet Wallet;

    // Estado por partida de los efectos (el asset del efecto se comparte, esto no).
    private readonly Dictionary<PassiveEffect, object> states = new Dictionary<PassiveEffect, object>();

    /// <summary>Estado propio de un efecto para ESTA partida (se crea al pedirlo).</summary>
    public T GetState<T>(PassiveEffect effect) where T : class, new()
    {
        if (states.TryGetValue(effect, out object existing))
            return (T)existing;

        var created = new T();
        states[effect] = created;
        return created;
    }

    public void ClearState(PassiveEffect effect) => states.Remove(effect);
}
