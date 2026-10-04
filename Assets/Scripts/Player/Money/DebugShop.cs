using UnityEngine;

public class DebugShop : MonoBehaviour
{
    [SerializeField] private PassiveItem[] shopItemList;
    private PlayerPassives playerPassives;
    private Wallet wallet;

    void Start()
    {
        playerPassives = gameObject.GetComponent<PlayerPassives>();
        wallet = gameObject.GetComponent<Wallet>();
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1) && wallet.TrySpend(1))
        {
            playerPassives.Add(shopItemList[0]);
        }
        if(Input.GetKeyDown(KeyCode.Alpha2) && wallet.TrySpend(1))
        {
            playerPassives.Add(shopItemList[1]);
        }
        if(Input.GetKeyDown(KeyCode.Alpha3) && wallet.TrySpend(1))
        {
            playerPassives.Add(shopItemList[2]);
        }
        if(Input.GetKeyDown(KeyCode.Alpha4) && wallet.TrySpend(1))
        {
            playerPassives.Add(shopItemList[3]);
        }
        if(Input.GetKeyDown(KeyCode.Alpha5) && wallet.TrySpend(1))
        {
            playerPassives.Add(shopItemList[4]);
        }
    }
}
