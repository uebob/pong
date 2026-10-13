using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hace que el objeto parpadee en blanco un instante al recibir daño (se engancha al evento
/// Health.Damaged, así que no hay que tocar EnemyHealth). Sirve igual para enemigos y para el jugador.
///
/// Durante el flash se sustituyen los materiales de las mallas por un material blanco SIN iluminación
/// (Unlit), que da un blanco puro sea cual sea el shader original; al terminar se restauran.
/// Se usan sharedMaterials, así que no se crean copias de material por enemigo.
///
/// Material: Create > Material, shader "Universal Render Pipeline/Unlit" (o "Unlit/Color" en el
/// pipeline clásico), color blanco y sin textura. Un solo material vale para todos los enemigos.
/// </summary>
public class DamageFlash : MonoBehaviour
{
    [Tooltip("Vida que se escucha. Si está vacío se busca en este objeto o en sus padres.")]
    [SerializeField] private Health health;

    [Tooltip("Material blanco Unlit con el que se sustituyen los materiales durante el flash.")]
    [SerializeField] private Material flashMaterial;

    [Tooltip("Duración del flash (s). Usa el tiempo del juego: si hay un freeze, el flash se ve durante él.")]
    [SerializeField] private float flashDuration = 0.08f;

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Material[][] flashMaterials;
    private Coroutine restoreRoutine;

    private void Awake()
    {
        if (health == null)
            health = GetComponentInParent<Health>();

        if (health == null)
            Debug.LogWarning($"[DamageFlash] '{name}' no encuentra ningún Health: no parpadeará.", this);

        if (flashMaterial == null)
            Debug.LogWarning($"[DamageFlash] '{name}' no tiene Flash Material asignado: no parpadeará.", this);

        CacheRenderers();
    }

    private void OnEnable()
    {
        if (health != null)
            health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Damaged -= OnDamaged;

        // Si se desactiva en pleno flash, no dejamos el material blanco puesto.
        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
        }
        SetMaterials(originalMaterials);
    }

    /// <summary>Guarda los materiales originales de cada malla y prepara los del flash.</summary>
    private void CacheRenderers()
    {
        var found = new List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            // Solo mallas: se ignoran trails, partículas, etc.
            if (r is MeshRenderer || r is SkinnedMeshRenderer)
                found.Add(r);
        }

        renderers = found.ToArray();
        originalMaterials = new Material[renderers.Length][];
        flashMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterials;

            // Una malla puede tener varios materiales: todos pasan a ser el blanco.
            var flash = new Material[originalMaterials[i].Length];
            for (int j = 0; j < flash.Length; j++)
                flash[j] = flashMaterial;
            flashMaterials[i] = flash;
        }
    }

    private void OnDamaged(float amount)
    {
        if (flashMaterial == null || renderers == null || renderers.Length == 0) return;

        // Si ya estaba parpadeando, se reinicia el temporizador.
        if (restoreRoutine != null)
            StopCoroutine(restoreRoutine);

        SetMaterials(flashMaterials);
        restoreRoutine = StartCoroutine(RestoreAfterDelay());
    }

    private IEnumerator RestoreAfterDelay()
    {
        yield return new WaitForSeconds(flashDuration);

        SetMaterials(originalMaterials);
        restoreRoutine = null;
    }

    private void SetMaterials(Material[][] materials)
    {
        if (renderers == null || materials == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue; // destruido (p. ej. el enemigo murió)
            renderers[i].sharedMaterials = materials[i];
        }
    }
}
