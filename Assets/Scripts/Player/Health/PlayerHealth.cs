using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float health;
    public float maxHealth;
    void Start()
    {
        health = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        Debug.Log("vida:" + health + "\ndaño: " + damage);
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
}
