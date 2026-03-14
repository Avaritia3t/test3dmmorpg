public interface IAttackHandlerPool
{
    void InitializePool();
    AttackHandlerV2 RequestHandler();
    void ReturnHandler(AttackHandlerV2 handler);
}
