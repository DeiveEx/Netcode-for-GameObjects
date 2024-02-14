using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HelloWorldManager : MonoBehaviour
{
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
            SubmitNewPosition();
        }
        
        GUILayout.EndArea();
    }
    
    private void StartButtons()
    {
        if (GUILayout.Button("Host"))
            NetworkManager.Singleton.StartHost();
        
        if (GUILayout.Button("Server"))
            NetworkManager.Singleton.StartServer();
        
        if (GUILayout.Button("Client"))
            NetworkManager.Singleton.StartClient();
    }
    
    private void StatusLabels()
    {
        string mode = "";

        if (NetworkManager.Singleton.IsHost)
            mode = "Host";
        
        if (NetworkManager.Singleton.IsServer)
            mode = "Server";
        
        if (NetworkManager.Singleton.IsClient)
            mode = "Client";
        
        GUILayout.Label($"Transport: {NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name}");
        GUILayout.Label($"Mode: {mode}");
    }

    private void SubmitNewPosition()
    {
        if(GUILayout.Button(NetworkManager.Singleton.IsServer ? "Move" : "Request position change"))
        {
            if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
            {
                foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).GetComponent<HelloWorldPlayer>().Move();
                }
            }
            else
            {
                var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                var player = playerObject.GetComponent<HelloWorldPlayer>();
                player.Move();
            }
        }
    }
}
