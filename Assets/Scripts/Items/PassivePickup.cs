using UnityEngine;

/// <summary>
/// Objeto pasivo en el mundo. Al pasar por encima (trigger), añade su PassiveItem al
/// PlayerPassives de quien lo toque y desaparece.
///
/// Un único prefab sirve para todos los items: asigna el PassiveItem y, si el item tiene
/// worldModel, el modelo se instancia solo dentro de "visualRoot", que gira y levita
/// (FloatingSpin). El trigger está en la raíz y no se mueve.
///
/// Prefab: GameObject vacío + un Collider con "Is Trigger" + este componente. No necesita Rigidbody
/// (el del jugador es el que dispara el trigger).
/// </summary>
public class PassivePickup : MonoBehaviour
{
    [SerializeField] private PassiveItem item;

    [Header("Visual")]
    [Tooltip("Hijo que se anima (giro y levitación). Si está vacío se crea uno automáticamente.\n" +
             "Si el item tiene worldModel, se instancia dentro. Si prefieres un modelo propio en este " +
             "prefab, ponlo como hijo de este objeto y deja el worldModel del item vacío.")]
    [SerializeField] private Transform visualRoot;

    [Header("Feedback (opcional)")]
    [SerializeField] private AudioClip pickupSound;
    [Tooltip("Se instancia en la posición del objeto al recogerlo (partículas, etc.).")]
    [SerializeField] private GameObject pickupEffect;

    public PassiveItem Item => item;

    private bool collected;
    private GameObject spawnedModel;

    private void Reset()
    {
        // Al añadir el componente: si ya hay un collider, lo dejamos como trigger.
        if (TryGetComponent(out Collider col)) col.isTrigger = true;
    }

    private void Awake()
    {
        if (!TryGetComponent(out Collider col) || !col.isTrigger)
            Debug.LogWarning($"[PassivePickup] '{name}' necesita un Collider con 'Is Trigger' activado.", this);

        BuildVisual();
    }

    /// <summary>Para tiendas, drops o spawners: cambia el item que representa este pickup.</summary>
    public void Setup(PassiveItem newItem)
    {
        item = newItem;
        BuildVisual();
    }

    private void BuildVisual()
    {
        if (visualRoot == null)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            visualRoot = visual.transform;
        }

        if (!visualRoot.TryGetComponent(out FloatingSpin _))
            visualRoot.gameObject.AddComponent<FloatingSpin>();

        // Si se cambia de item, quitamos el modelo anterior.
        if (spawnedModel != null)
        {
            spawnedModel.SetActive(false);
            Destroy(spawnedModel);
            spawnedModel = null;
        }

        if (item != null && item.worldModel != null)
            spawnedModel = Instantiate(item.worldModel, visualRoot);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || item == null) return;

        // Solo lo recoge quien tenga PlayerPassives (el jugador).
        PlayerPassives passives = other.GetComponentInParent<PlayerPassives>();
        if (passives == null) return;

        // Si no se puede añadir (p. ej. item no apilable que ya tienes), se queda en el suelo.
        if (!passives.Add(item)) return;

        collected = true;

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
