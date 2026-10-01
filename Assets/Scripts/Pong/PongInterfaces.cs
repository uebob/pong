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
/// mirando source.Faction / source.Owner (p. ej. un pong enemigo parreado SÍ debe
/// dañar a enemigos, pero quizá no a quien lo lanzó si así lo quieres).
/// </summary>
public interface IPongDamageable
{
    void TakeDamage(float amount, PongProjectile source);
}