using UnityEngine;
using TMPro;
using System.Collections;
public class DebugText : MonoBehaviour
{
    [SerializeField] private TMP_Text playerStatsText;
    [SerializeField] private TMP_Text pongsText;
    [SerializeField] private TMP_Text passivesText;
    private Rigidbody rb;
    private PlayerHealth playerHealth;
    private PongInventory inventory;
    private PlayerPassives pasivos;
    private Wallet wallet;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        playerHealth = gameObject.GetComponent<PlayerHealth>();
        inventory = gameObject.GetComponent<PongInventory>();
        pasivos = gameObject.GetComponent<PlayerPassives>();
        wallet = gameObject.GetComponent<Wallet>();
    }

    // Update is called once per frame
    void Update()
    {
        float speed = Mathf.Round(rb.linearVelocity.magnitude * 100f) / 100f;
        float yspeed = Mathf.Round(rb.linearVelocity.y * 100f) / 100f;
        if (Mathf.Abs(speed) < 0.01f) speed = 0;
        if (Mathf.Abs(yspeed) < 0.01f) yspeed = 0;

        playerStatsText.text = "health: " + playerHealth.health + "\nmoney: " + wallet.Money + "\n speed: " + speed + "\n y speed: " + yspeed;

        pongsText.text = "Pongs:\n";
        if(inventory.queue.Count > 0)
        {
            foreach (var pong in inventory.queue)
            {
                pongsText.text += pong.Definition.displayName + "\n";
            }
        }

        if(pasivos.ActivePassives.Count > 0)
        {
            passivesText.text = "pasivos: ";
            foreach (var pasivo in pasivos.ActivePassives)
            {
                passivesText.text += pasivo.displayName + ", ";
            }
        }
    }
}
