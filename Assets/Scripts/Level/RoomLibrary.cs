using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define qué prefabs de sala existen para cada tipo.
/// Crear con: clic derecho en Project > Create > Dungeon > Room Library
/// </summary>
[CreateAssetMenu(menuName = "Dungeon/Room Library", fileName = "RoomLibrary")]
public class RoomLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public RoomType type;
        public Room[] prefabs;
    }

    public Entry[] entries;

    /// <summary>
    /// Rellena 'results' con los prefabs del tipo dado que pueden tener
    /// exactamente las puertas 'requiredMask' (rotándolos si se permite).
    /// </summary>
    public void GetMatchingPrefabs(RoomType type, int requiredMask, List<Room> results)
    {
        results.Clear();
        var steps = new List<int>(4);

        foreach (var entry in entries)
        {
            if (entry.type != type || entry.prefabs == null) continue;

            foreach (var prefab in entry.prefabs)
            {
                if (prefab == null) continue;

                prefab.GetValidRotations(requiredMask, steps);
                if (steps.Count > 0)
                    results.Add(prefab);
            }
        }
    }
}
