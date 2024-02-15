using System;
using Ignix.Debug.Console;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using UnityEngine;

public class TestRelay : MonoBehaviour
{
    [SerializeField] private string playerName = "Player_0";
    
    private async void Start()
    {
        if (!string.IsNullOrEmpty(playerName))
        {
            var initializationOptions = new InitializationOptions();
            initializationOptions.SetProfile(playerName);
            await UnityServices.InitializeAsync(initializationOptions);
        }
        else
        {
            await UnityServices.InitializeAsync();
        }

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed In with ID: {AuthenticationService.Instance.PlayerId}");
        };
        
        await AuthenticationService.Instance.SignInAnonymouslyAsync(); //TODO Use Steam account
    }
    
    [DevCommand]
    private async void CreateRelay()
    {
        try
        {
            //Creates an allocation on a Relay server
            var allocation = await RelayService.Instance.CreateAllocationAsync(3); //Number of connections WITHOUT the host.
            
            //Get the join code from the relay server to send to your friends
            var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Join Code: {joinCode}");

            //Open a connection using the Relay
            var relayServerData = new RelayServerData(allocation, "dtls"); //dtls is the type of connection. Could be "udp" too, for example
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }

    [DevCommand]
    private async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"Joining Relay with code {joinCode}");
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            var relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }
}
