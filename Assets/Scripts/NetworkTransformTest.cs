using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkTransformTest : NetworkBehaviour
{
    private void Update()
    {
        if (IsOwner)
        {
            Vector3 dir = Vector3.zero;

            if (Input.GetKey(KeyCode.A))
                dir.x = -1;
            
            if (Input.GetKey(KeyCode.D))
                dir.x = 1;
            
            if (Input.GetKey(KeyCode.W))
                dir.z = 1;
            
            if (Input.GetKey(KeyCode.S))
                dir.z = -1;

            transform.position += dir * (3f * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Space))
                Test();
            
            if (Input.GetKeyDown(KeyCode.Z))
                Test2();
        }
    }

    private void Test()
    {
        if (IsServer)
        {
            Debug.Log("This is the server. No Rpc Needed");
        }
        else
        {
            Test_ServerRpc(10);
        }
    }

    [ServerRpc]
    private void Test_ServerRpc(int value)
    {
        Debug.Log($"Server received a client request with the value: {value}");
    }

    private void Test2()
    {
        if (IsServer)
        {
            Debug.Log("Sending Rpc to clients...");
            Test2_ClientRpc();
        }
        else
        {
            Debug.Log("This is a client. Cannot do a ClientRpc");
        }
    }
    
    [ClientRpc]
    private void Test2_ClientRpc()
    {
        Debug.Log("Client received a Rpc from the server");
    }
}
