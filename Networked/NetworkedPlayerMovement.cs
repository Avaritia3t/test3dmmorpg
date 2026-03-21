// Server-authoritative movement (Cmd + NavMesh only on server). Disabled for Mirror + ParrelSync prototyping:
// use local NavMeshAgent.SetDestination + NetworkTransform on the player instead.
// To re-enable: change #if false to #if true, add this component to the player prefab, and wire NetworkedDomainController.ClickToMove to RequestMove().

#if false
using UnityEngine;
using UnityEngine.AI;
using Mirror;

/// <summary>
/// Server-authoritative movement: NavMeshAgent runs only on the server; local player sends <see cref="CmdSetDestination"/>.
/// Add <see cref="NetworkTransform"/> (or NetworkTransformUnreliable) on the player prefab so clients receive synced transforms.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkedPlayerMovement : NetworkBehaviour
{
    [SerializeField] private float maxMoveCommandDistance = 120f;
    [SerializeField] private float navMeshSampleRadius = 12f;

    private NavMeshAgent _agent;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        // Remote clients: no local simulation — position comes from NetworkTransform.
        if (NetworkClient.active && !NetworkServer.active)
            _agent.enabled = false;
    }

    /// <summary>Called from click-to-move (local player). Offline: applies directly to NavMeshAgent.</summary>
    public void RequestMove(Vector3 worldDestination)
    {
        if (_agent == null)
            _agent = GetComponent<NavMeshAgent>();

        if (!NetworkClient.active && !NetworkServer.active)
        {
            if (!_agent.enabled)
                _agent.enabled = true;
            if (NavMesh.SamplePosition(worldDestination, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
            return;
        }

        if (!isLocalPlayer)
            return;

        CmdSetDestination(worldDestination);
    }

    [Command]
    private void CmdSetDestination(Vector3 worldDestination)
    {
        if (_agent == null)
            _agent = GetComponent<NavMeshAgent>();
        if (_agent == null || !_agent.enabled)
            return;

        Vector3 flat = worldDestination - transform.position;
        if (flat.sqrMagnitude > maxMoveCommandDistance * maxMoveCommandDistance)
            return;

        if (NavMesh.SamplePosition(worldDestination, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }
}
#endif
