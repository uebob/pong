using System;
using UnityEngine;

/// <summary>
/// Cartera del jugador. Es el ÚNICO punto por el que entra o sale dinero: ningún otro script
/// debe tocar el saldo, solo llamar a Add / TrySpend. Así los bonus (MoneyGain), los eventos
/// y las reglas futuras (tienda, límites, guardado...) viven en un solo sitio.
/// Ponlo en el jugador. Quien consuma dinero (una tienda, por ejemplo) usa TrySpend.
/// </summary>
public class Wallet : MonoBehaviour
{
    [SerializeField] private int startingMoney = 0;

    /// <summary>Saldo actual. Solo se modifica desde esta clase.</summary>
    [SerializeField] public int Money;

    /// <summary>
    /// Se dispara cuando cambia el saldo: (saldo nuevo, variación; negativa si se gasta).
    /// Para la HUD, sonidos, efectos...
    /// </summary>
    public event Action<int, int> Changed;

    private PlayerPassives passives;
    private float gainRemainder; // fracción acumulada de bonus (p. ej. +25 % de 1 moneda = 0.25)

    private void Awake()
    {
        passives = GetComponentInParent<PlayerPassives>(); // opcional: sin él no hay bonus
        Money = Mathf.Max(0, startingMoney);
    }

    /// <summary>
    /// Ingresa dinero. Aplica los pasivos de ganancia (PlayerStat.MoneyGain) salvo que
    /// applyBonuses sea false (p. ej. para devolver dinero). Devuelve lo realmente ingresado.
    /// </summary>
    public int Add(int amount, bool applyBonuses = true)
    {
        if (amount <= 0) return 0;

        float gained = amount;
        if (applyBonuses && passives != null)
            gained = passives.Apply(PlayerStat.MoneyGain, amount);

        // El bonus puede dar fracciones (+25 % de 1 moneda): se acumulan para no perderlas.
        gained += gainRemainder;
        int whole = Mathf.Max(0, Mathf.FloorToInt(gained));
        gainRemainder = Mathf.Max(0f, gained - whole);

        if (whole == 0) return 0;

        Money += whole;
        Changed?.Invoke(Money, whole);
        return whole;
    }

    public bool CanAfford(int amount) => amount <= Money;

    /// <summary>Gasta dinero si hay suficiente. Devuelve false (y no cambia nada) si no.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0 || amount > Money) return false;
        if (amount == 0) return true;

        Money -= amount;
        Changed?.Invoke(Money, -amount);
        return true;
    }
}
