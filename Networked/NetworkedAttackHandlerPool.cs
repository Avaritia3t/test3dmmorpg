using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pools NetworkedAttackHandlerController instances. Register as INetworkedAttackHandlerPool when using the Networked stack.
/// </summary>
public class NetworkedAttackHandlerPool : MonoBehaviour, INetworkedAttackHandlerPool
{
    [SerializeField] private GameObject attackHandlerPrefab;
    [SerializeField] private int initialPoolSize = 10;

    private Queue<NetworkedAttackHandlerController> pool = new Queue<NetworkedAttackHandlerController>();

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
            CreateNewHandler();
    }

    private void CreateNewHandler()
    {
        GameObject handlerObject = Instantiate(attackHandlerPrefab);
        var handler = handlerObject.GetComponent<NetworkedAttackHandlerController>();
        if (handler == null)
        {
            Debug.LogError("[NetworkedAttackHandlerPool] Prefab must have NetworkedAttackHandlerController.");
            Destroy(handlerObject);
            return;
        }
        handlerObject.SetActive(false);
        pool.Enqueue(handler);
    }

    public NetworkedAttackHandlerController RequestHandler()
    {
        if (pool.Count > 0)
        {
            var handler = pool.Dequeue();
            handler.gameObject.SetActive(true);
            return handler;
        }
        CreateNewHandler();
        return pool.Count > 0 ? pool.Dequeue() : null;
    }

    public void ReturnHandler(NetworkedAttackHandlerController handler)
    {
        if (handler == null) return;
        handler.gameObject.SetActive(false);
        pool.Enqueue(handler);
    }
}
