using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

using Mirror;
using Mirror.Discovery;
namespace Wugou.Multiplayer
{
    public struct MultiplayerServerRequest : NetworkMessage { }

    public struct MultiplayerServerResponse : NetworkMessage
    {
        // The server that sent this
        // this is a property so that it is not serialized,  but the
        // client fills this up after we receive it
        public IPEndPoint EndPoint { get; set; }

        public Uri uri;

        // Prevent duplicate server appearance when a connection can be made via LAN on multiple NICs
        public long serverId;

        // gamepaly info
        public string playerName;
        public string gameMap;
        public int curPlayerCount;
        public int maxPlayerCount;
    }

    [DisallowMultipleComponent]
    public class MultiplayerNetworkDiscovery : NetworkDiscoveryBase<MultiplayerServerRequest, MultiplayerServerResponse>
    {
        #region Server

        /// <summary>
        /// Process the request from a client
        /// </summary>
        /// <remarks>
        /// Override if you wish to provide more information to the clients
        /// such as the name of the host player
        /// </remarks>
        /// <param name="request">Request coming from client</param>
        /// <param name="endpoint">Address of the client that sent the request</param>
        /// <returns>The message to be sent back to the client or null</returns>
        protected override MultiplayerServerResponse ProcessRequest(MultiplayerServerRequest request, IPEndPoint endpoint)
        {
            // In this case we don't do anything with the request
            // but other discovery implementations might want to use the data
            // in there,  This way the client can ask for
            // specific game mode or something

            try
            {
                // this is an example reply message,  return your own
                // to include whatever is relevant for your game
                return new MultiplayerServerResponse
                {
                    serverId = ServerId,
                    uri = transport.ServerUri(),
                    playerName = GamePlay.loginInfo.name,
                    gameMap = MultiplayerGameManager.instance.selectedGameMap.name,
                    curPlayerCount = MultiplayerGameManager.instance.roomplayers.Count,
                    maxPlayerCount = MultiplayerGameManager.instance.gameMap.maxPlayerCount
                };
            }
            catch (NotImplementedException)
            {
                Debug.LogError($"Transport {transport} does not support network discovery");
                throw;
            }
        }

        #endregion

        #region Client

        /// <summary>
        /// Create a message that will be broadcasted on the network to discover servers
        /// </summary>
        /// <remarks>
        /// Override if you wish to include additional data in the discovery message
        /// such as desired game mode, language, difficulty, etc... </remarks>
        /// <returns>An instance of ServerRequest with data to be broadcasted</returns>
        protected override MultiplayerServerRequest GetRequest() => new MultiplayerServerRequest();

        /// <summary>
        /// Process the answer from a server
        /// </summary>
        /// <remarks>
        /// A client receives a reply from a server, this method processes the
        /// reply and raises an event
        /// </remarks>
        /// <param name="response">Response that came from the server</param>
        /// <param name="endpoint">Address of the server that replied</param>
        protected override void ProcessResponse(MultiplayerServerResponse response, IPEndPoint endpoint)
        {
            // we received a message from the remote endpoint
            response.EndPoint = endpoint;

            // although we got a supposedly valid url, we may not be able to resolve
            // the provided host
            // However we know the real ip address of the server because we just
            // received a packet from it,  so use that as host.
            UriBuilder realUri = new UriBuilder(response.uri)
            {
                Host = response.EndPoint.Address.ToString()
            };
            response.uri = realUri.Uri;

            OnServerFound.Invoke(response);
        }

        #endregion

    }
}

