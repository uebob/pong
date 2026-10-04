using UnityEngine;

/// <summary>
/// Moneda recogible. Necesita un Collider con "Is Trigger" activado.
/// Al tocarla, cualquier objeto que tenga una Wallet (el jugador) la recoge: se ingresa su
/// valor con Wallet.Add y la moneda desaparece. No hace falta tag ni Rigidbody en la moneda
/// (el jugador ya tiene Rigidbody, que es lo que necesita Unity para disparar el trigger).
/// </summary>
public class Coin : MonoBehaviour
{
    [Tooltip("Dinero que da al recogerla (antes de bonus).")]
    [SerializeField] private int value = 1;

    [Header("Feedback (opcional)")]
    [SerializeField] private AudioClip pickupSound;
    [Tooltip("Se instancia en la posición de la moneda al recogerla (partículas, etc.).")]
    [SerializeField] private GameObject pickupEffect;
    [Tooltip("Giro sobre el eje vertical (grados/s). 0 = quieta.")]
    [SerializeField] private float spinSpeed = 120f;

    private bool collected;

    private void Reset()
    {
        // Al añadir el componente: si ya hay un collider, lo dejamos como trigger.
        if (TryGetComponent(out Collider col)) col.isTrigger = true;
    }

    private void Awake()
    {
        if (!TryGetComponent(out Collider col) || !col.isTrigger)
            Debug.LogWarning($"[Coin] '{name}' necesita un Collider con 'Is Trigger' activado para poder recogerse.", this);
    }

    private void Update()
    {
        if (spinSpeed != 0f)
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        // Solo la recoge quien tenga cartera: el jugador (no pongs, enemigos...).
        Wallet wallet = other.GetComponentInParent<Wallet>();
        if (wallet == null) return;

        collected = true;
        wallet.Add(value);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
