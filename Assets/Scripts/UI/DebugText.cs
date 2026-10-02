using UnityEngine;
using TMPro;
using System.Collections;
public class DebugText : MonoBehaviour
{
    [SerializeField] private TMP_Text playerStatsText;
    [SerializeField] private TMP_Text pongsText;
    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PongInventory inventory;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        playerHealth = gameObject.GetComponent<PlayerHealth>();
        inventory = gameObject.GetComponent<PongInventory>();
    }

    // Update is called once per frame
    void Update()
    {
        float speed = Mathf.Round(rb.linearVelocity.magnitude * 100f) / 100f;
        if (Mathf.Abs(speed) < 0.01f) speed = 0;

        playerStatsText.text = "health: " + playerHealth.health + "\n speed: " + speed;

        pongsText.text = "Pongs:\n";
        foreach (var pong in inventory.queue)
        {
            pongsText.text += pong.Definition.displayName + "\n";
        }
    }
}
