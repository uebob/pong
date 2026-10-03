using System.Collections.Generic;

/// <summary>
/// Cualquier cosa que pueda ser dueña de pongs (jugador, enemigos) y aplicarles pasivos.
/// El proyectil busca este componente en su Owner al resolver sus estadísticas.
/// </summary>
public interface IPongModifierProvider
{
    IReadOnlyList<PassiveItem> ActivePassives { get; }
}

/// <summary>
/// Bando de quien lanzó el pong. Se fija al crearlo y no cambia nunca (ni al parrearlo).
/// </summary>
public enum PongFaction { Player, Enemy }

/// <summary>
/// Implementar en enemigos, jugador, objetos destruibles...
/// Los behaviors no filtran a quién dañan: es el RECEPTOR quien decide en TakeDamage,
/// mirando source.Owner si algún día quieres filtrar. Ahora mismo no se filtra nada:
/// cualquier pong daña a cualquiera, incluido su propio dueño. La facción (Faction) solo
/// sirve para que el pong elija objetivo; los objetos pasivos los decide el Owner.
/// </summary>
public interface IPongDamageable
{
    void TakeDamage(float amount, PongProjectile source);
}