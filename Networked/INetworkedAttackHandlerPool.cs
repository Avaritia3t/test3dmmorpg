/// <summary>Pool for NetworkedAttackHandlerController. Implemented by NetworkedAttackHandlerPool; required in scene and registered by CombatStartup.</summary>
public interface INetworkedAttackHandlerPool
{
    void InitializePool();
    NetworkedAttackHandlerController RequestHandler();
    void ReturnHandler(NetworkedAttackHandlerController handler);
}
