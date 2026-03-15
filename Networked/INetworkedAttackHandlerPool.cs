public interface INetworkedAttackHandlerPool
{
    void InitializePool();
    NetworkedAttackHandlerController RequestHandler();
    void ReturnHandler(NetworkedAttackHandlerController handler);
}
