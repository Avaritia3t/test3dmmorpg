# Networked

First-line network rewrite targets, duplicated and renamed to the **Networked<Component>Controller** pattern. Use these as the base for Mirror-based implementation.

| Original | Networked copy |
|----------|----------------|
| `Player/DomainControllerV3.cs` | `NetworkedDomainController.cs` |
| `World/SubdomainV2.cs` | `NetworkedSubdomainController.cs` |
| `Core/CameraController.cs` | `NetworkedCameraController.cs` |
| `Core/GameBootstrap.cs` | `NetworkedGameBootstrap.cs` |
| `World/MapManagerV3.cs` | `NetworkedMapController.cs` |
| `Scene/H1SceneManager.cs` | `NetworkedH1SceneController.cs` |
| `Scene/N1SceneManager.cs` | `NetworkedN1SceneController.cs` |
| (new) | `NetworkedPlayerCombatHelperController.cs` |
| (new) | `NetworkedAttackHandlerController.cs`, `NetworkedAttackHandlerPool.cs`, `INetworkedAttackHandlerPool.cs` |

**Notes:**

- `NetworkedSubdomainController` uses `SubdomainV2Type` and `SubdomainState` from `World/SubdomainV2.cs` (not duplicated).
- Cross-references within this folder use the Networked* types (e.g. `NetworkedMapController` expects `NetworkedDomainController` on the player).
- **Attack handler:** `Combat/AttackHandlerV2.cs` is unchanged (local stack). The Networked stack uses `NetworkedAttackHandlerController` and `INetworkedAttackHandlerPool`; CombatStartup auto-registers the pool when present. Networked scene controllers initialize it.
- **Scene setup (Networked):** Add a GameObject with `NetworkedAttackHandlerPool`; assign a prefab that has only `NetworkedAttackHandlerController` to its `attackHandlerPrefab` field.
- To use `NetworkedMapController` as `IMapService`, register it in the locator (e.g. a Networked-specific startup or scene setup).
- **Player setup:** Add both `NetworkedDomainController` and `NetworkedPlayerCombatHelperController` to the player GameObject when using the Networked stack.
