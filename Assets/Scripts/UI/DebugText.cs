using UnityEngine;
using TMPro;
public class DebugText : MonoBehaviour
{
    [SerializeField] private TMP_Text playerStatsText;
    private Rigidbody rb;
    private PlayerHealth playerHealth;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        playerHealth = gameObject.GetComponent<PlayerHealth>();
    }

    // Update is called once per frame
    void Update()
    {
        float speed = Mathf.Round(rb.linearVelocity.magnitude * 100f) / 100f;
        if (Mathf.Abs(speed) < 0.01f) speed = 0;
        playerStatsText.text = "health: " + playerHealth.health + "\n speed: " + speed;
    }
}
