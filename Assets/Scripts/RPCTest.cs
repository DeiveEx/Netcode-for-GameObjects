using Unity.Netcode;
using UnityEngine;

public class RPCTest : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer && IsOwner)
        {
            TestServerRPC(0, NetworkObjectId);
        }
    }
    
    [ClientRpc]
    private void TestClientRPC(int value, ulong sourceNetworkObjectID)
    {
        Debug.Log($"Client received the RPC #{value} on NetworkObjectID #{sourceNetworkObjectID}");

        if (IsOwner)
        {
            TestServerRPC(value + 1, sourceNetworkObjectID);
        }
    }
    
    [ServerRpc]
    private void TestServerRPC(int value, ulong sourceObjectID)
    {
        Debug.Log($"Server received the RPC #{value} one NetworkObject #{sourceObjectID}");
        TestClientRPC(value, sourceObjectID);
    }
}
