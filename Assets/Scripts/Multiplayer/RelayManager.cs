using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{
    public Button hostButton;
    public InputField codeText;
    public Button joinButton;
    public InputField joinInput;
    public static List<CharacterMover> players = new List<CharacterMover>();

    async void Start()
    {
        //Enable unity services
        await UnityServices.InitializeAsync();
        //Authenticate current unity instance as an anonymous user to be able to access unity service's servers
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        //Subscribing to button events
        hostButton.onClick.AddListener(CreateRelay);
        joinButton.onClick.AddListener(() => JoinRelay(joinInput.text));
    }

    /// <summary>
    /// Create new room
    /// </summary>
    async void CreateRelay()
    {
        //Request for a new room using (3) maximum players (connections)
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
        //Using created room, get join code for other players
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        codeText.text = joinCode;
        //Setup connection type
        var relayServerData = AllocationUtils.ToRelayServerData(allocation, RelayProtocol.UDP);

        //Tell unity transport what kind of connection to make, and to what room
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        //Start the room as the host player
        NetworkManager.Singleton.StartHost();
    }

    /// <summary>
    /// Join existing room
    /// </summary>
    /// <param name="joinCode">Host room code</param>
    async void JoinRelay(string joinCode)
    {
        //Request join an existing room using the provided code
        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        //Setup connection type
        var relayServerData = AllocationUtils.ToRelayServerData(allocation, RelayProtocol.UDP);

        //Tell unity transport what kind of connection to make, and to what room
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        //Join the room as a guest player
        NetworkManager.Singleton.StartClient();
    }


}
