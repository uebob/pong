using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Una puerta de este prefab y hacia qué lado (en el prefab sin rotar) mira.</summary>
[Serializable]
public class DoorSlot
{
    public Direction direction;
    public Door door;
}

/// <summary>
/// Va en la raíz de CADA prefab de sala. El prefab ya incluye sus paredes, marcos, etc.
/// Aquí solo declaramos qué puertas tiene (lista doorSlots) y si puede rotarse.
/// El pivote del prefab debe estar en el CENTRO de la sala.
/// </summary>
public class Room : MonoBehaviour
{
    [Tooltip("Una entrada por cada puerta del prefab, con el lado al que mira (N=+Z, E=+X, S=-Z, W=-X).")]
    [SerializeField] private DoorSlot[] doorSlots;

    [Tooltip("Permite que el builder rote este prefab en pasos de 90° para encajar con el layout.")]
    [SerializeField] private bool canRotate = true;

    public RoomData Data { get; private set; }
    public int RotationSteps { get; private set; }
    public bool CanRotate => canRotate;

    // ------------------------------------------------------------------
    // Máscaras de puertas (bit i = Direction i: N=1, E=2, S=4, W=8)
    // ------------------------------------------------------------------

    /// <summary>Puertas de este prefab tal como está diseñado (sin rotar).</summary>
    public int GetDoorMask()
    {
        int m = 0;
        if (doorSlots == null) return m;
        foreach (var slot in doorSlots)
            m |= 1 << (int)slot.direction;
        return m;
    }

    /// <summary>Rota una máscara 'steps' pasos de 90° en sentido horario (vista desde arriba): N->E->S->W.</summary>
    public static int RotateMask(int mask, int steps)
    {
        int result = 0;
        for (int d = 0; d < 4; d++)
            if ((mask & (1 << d)) != 0)
                result |= 1 << ((d + steps) % 4);
        return result;
    }

    /// <summary>Rellena 'steps' con las rotaciones (0-3) con las que este prefab tiene exactamente las puertas 'requiredMask'.</summary>
    public void GetValidRotations(int requiredMask, List<int> steps)
    {
        steps.Clear();
        int baseMask = GetDoorMask();
        int maxSteps = canRotate ? 4 : 1;

        for (int r = 0; r < maxSteps; r++)
            if (RotateMask(baseMask, r) == requiredMask)
                steps.Add(r);
    }

    public static string DescribeMask(int mask)
    {
        if (mask == 0) return "(sin puertas)";
        string[] names = { "N", "E", "S", "W" };
        var parts = new List<string>();
        for (int d = 0; d < 4; d++)
            if ((mask & (1 << d)) != 0) parts.Add(names[d]);
        return string.Join("+", parts);
    }

    // ------------------------------------------------------------------
    // Uso en juego
    // ------------------------------------------------------------------

    public void Initialize(RoomData data, int rotationSteps)
    {
        Data = data;
        RotationSteps = rotationSteps;
    }

    /// <summary>Puerta que mira hacia 'worldDir' en el mundo (tiene en cuenta la rotación aplicada).</summary>
    public Door GetDoor(Direction worldDir)
    {
        int localDir = ((int)worldDir - RotationSteps + 4) % 4;
        foreach (var slot in doorSlots)
            if ((int)slot.direction == localDir)
                return slot.door;
        return null;
    }

    public void SetDoorsOpen(bool open)
    {
        foreach (var slot in doorSlots)
        {
            if (slot.door == null) continue;
            if (open) slot.door.Open();
            else slot.door.Close();
        }
    }
}
