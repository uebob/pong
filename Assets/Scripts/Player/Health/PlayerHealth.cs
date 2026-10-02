using UnityEngine;
using System.Collections;
public class PlayerHealth : MonoBehaviour
{
    public float health;
    public float maxHealth;
    private PongInventory inventory;
    private PlayerParry playerParry;

    [SerializeField] private float collisionGracePeriod = 0.1f;
    private PongProjectile parriedPong;

    void Start()
    {
        health = maxHealth;
        inventory = gameObject.GetComponent<PongInventory>();
        playerParry = gameObject.GetComponent<PlayerParry>();
        playerParry.ParrySucceeded += OnParrySucceeded;
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if(health < 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    private void OnParrySucceeded(PongProjectile pong)
    {
        parriedPong = pong;
    }

    void OnCollisionEnter(Collision collision)
    {
        if(!collision.gameObject.CompareTag("Pong")) return;

        PongProjectile pong = collision.gameObject.GetComponent<PongProjectile>();
        
        StartCoroutine(WaitForParry(pong));
    }

    IEnumerator WaitForParry(PongProjectile pong)
    {
        yield return new WaitForSecondsRealtime(collisionGracePeriod);

        if (parriedPong == pong)
        {
            parriedPong = null;
            yield break;
        }

        TakeDamage(pong.speed);
        inventory.TryStore(pong);
    }
}
