using Mirror;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Optional: fires when this object is the local player (UI can subscribe in inspector).
/// Attach to player prefab with <see cref="NetworkIdentity"/>.
/// </summary>
[RequireComponent(typeof(NetworkIdentity))]
public class NetworkedUIBinder : MonoBehaviour
{
    [SerializeField]
    private UnityEvent onLocalPlayerReady;

    private void Start()
    {
        var ni = GetComponent<NetworkIdentity>();
        if (ni != null && ni.isLocalPlayer)
            onLocalPlayerReady?.Invoke();
    }
}
