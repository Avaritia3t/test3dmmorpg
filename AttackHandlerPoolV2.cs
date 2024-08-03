using System.Collections.Generic;
using UnityEngine;

public class AttackHandlerPoolV2 : MonoBehaviour
{
    public static AttackHandlerPoolV2 Instance { get; private set; }

    [SerializeField] private GameObject attackHandlerPrefab;
    [SerializeField] private int initialPoolSize = 10;

    private Queue<AttackHandlerV2> pool = new Queue<AttackHandlerV2>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewHandler();
        }
    }

    private void CreateNewHandler()
    {
        GameObject handlerObject = Instantiate(attackHandlerPrefab);
        AttackHandlerV2 handler = handlerObject.GetComponent<AttackHandlerV2>();
        handlerObject.SetActive(false);
        pool.Enqueue(handler);
    }

    public AttackHandlerV2 RequestHandler()
    {
        if (pool.Count > 0)
        {
            AttackHandlerV2 handler = pool.Dequeue();
            handler.gameObject.SetActive(true);
            return handler;
        }
        else
        {
            CreateNewHandler();
            return pool.Dequeue();
        }
    }

    public void ReturnHandler(AttackHandlerV2 handler)
    {
        handler.gameObject.SetActive(false);
        pool.Enqueue(handler);
    }
}