using System.Linq;
using UnityEngine;

public class SpawnEnemies : MonoBehaviour
{
    [SerializeField] private GameObject[] enemies;

    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.F))
        {
            int random = Random.Range(0, enemies.Length);
            Instantiate(enemies[random]);
        }
    }
}
