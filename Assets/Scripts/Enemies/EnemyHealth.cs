using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float health;
    public float maxHealth;
    void Start()
    {
        health = maxHealth;
    }

    void Update()
    {
        
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
