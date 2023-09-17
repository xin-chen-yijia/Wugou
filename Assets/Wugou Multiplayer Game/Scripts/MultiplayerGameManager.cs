using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
using Wugou.UI;
using Wugou.MapEditor;
using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

using Mirror;
using Mirror.Discovery;

namespace Wugou.Multiplayer
{
    /// <summary>
    /// ����Mirror�Ķ���ѵ��ϵͳ
    /// </summary>
    public class MultiplayerGameManager : NetworkRoomManager
    {
        public static MultiplayerGameManager instance => (NetworkRoomManager.singleton as MultiplayerGameManager);

        [Header("MultiplayerGame")]
        public MultiplayerNetworkDiscovery networkDiscovery;

        Dictionary<MultiplayerServerResponse, float> discoveredServers_ = new Dictionary<MultiplayerServerResponse, float>();   // ���ֵķ�����

        // ui
        public UIRootWindow uiRootWindow;

        /// <summary>
        /// ��ʶ�Ƿ�������Ϸ
        /// </summary>
        public bool isStartedGameplay { get; protected set; }

        /// <summary>
        /// �ڷ����е����
        /// </summary>
        public Dictionary<int, MultiplayerGameRoomPlayer> roomplayers { get; private set; } = new Dictionary<int, MultiplayerGameRoomPlayer>();

        /// <summary>
        /// ��¼��ǰ���
        /// </summary>
        public Dictionary<int, MultiplayerGamePlayer> gameplayers { get; private set; } = new Dictionary<int, MultiplayerGamePlayer>();

        /// <summary>
        /// ��ǰѡ��ĵ�ͼ
        /// </summary>
        public GameMap gameMap { get; protected set; }

        #region UnityFunctions

        public override void Awake()
        {
            base.Awake();

            GamePlay.isGaming = true;
        }

        public override void Start()
        {
            base.Start();

            //
            // SceneObject Type Prefab
            GameObject[] typePrefabs = Resources.LoadAll<GameObject>("SceneObjectPrototype/Gameplay");
            foreach (var v in typePrefabs)
            {
                GameEntityManager.RegisterPrefab(v.name, v);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            //
            GamePlay.isGaming = false;
        }

        public override void OnApplicationQuit()
        {
            base.OnApplicationQuit();

            // 
            UnloadGameMap();
        }

        #endregion

        #region Mirror Functions

        /// <summary>
        /// �����������ҽ���ʱ���ͣ��ű���Ϣ������ͬ�����ؽű�
        /// </summary>
        public struct GameMapMessage : NetworkMessage
        {
            public string content;
        }

        /// <summary>
        /// ����˵����ʼ��Ϸʱ����
        /// </summary>
        public struct StartGameMessage : NetworkMessage { }

        /// <summary>
        /// �ͻ��˼�������Ϸ����ʱ���� 
        /// </summary>
        public struct LoadedGameSceneMessage : NetworkMessage
        {
        }

        /// <summary>
        /// ��������ȫ���ͻ��������Ϸ��������ʱ����
        /// </summary>
        public struct ReadyGoMessage : NetworkMessage
        {
        }



        public override void OnRoomStartServer()
        {
            print("OnRoomStartServer");
            base.OnRoomStartServer();

        }


        public override void OnRoomStartClient()
        {
            base.OnRoomStartClient();

            // �������Ϣ����
            NetworkServer.RegisterHandler<LoadedGameSceneMessage>(OnLoadedGameSceneInternal, false);

            // �ͻ�����Ϣ���� 
            NetworkClient.RegisterHandler<GameMapMessage>(OnReceiveGameMapInternal, false);
            NetworkClient.RegisterHandler<StartGameMessage>(OnStartGameInternal, false);
            NetworkClient.RegisterHandler<ReadyGoMessage>(OnReadyGoInternal, false);

            print("OnRoomStartClient");
        }

        public override void OnRoomClientConnect()
        {
            base.OnRoomClientConnect();

            print("OnRoomClientConnect");
        }


        /// <summary>
        /// Called on the server when a client adds a new player with NetworkClient.AddPlayer.
        /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            print("OnServerAddPlayer");
            base.OnServerAddPlayer(conn);
        }

        public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnectionToClient conn)
        {
            conn.Send<GameMapMessage>(new GameMapMessage { content = selectedGameMap.rawContent });

            return base.OnRoomServerCreateRoomPlayer(conn);
        }

        /// <summary>
        /// Called on the server when a client is ready.
        /// <para>The default implementation of this function calls NetworkServer.SetClientReady() to continue the network setup process.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            print("OnServerReady");
            // This fires from a Ready message client sends to server after loading the online scene
            base.OnServerReady(conn);

            if (conn.identity == null)
            {
                //StartCoroutine(AddPlayerDelayed(conn));   // 
            }
        }

        //IEnumerator AddPlayerDelayed(NetworkConnectionToClient conn)
        //{
        //    // Wait for server to async load all subscenes for game instances
        //    while (!isStartedGameplay)
        //        yield return null;

        //    //// Send Scene msg to client telling it to load the first additive scene
        //    //conn.Send(new SceneMessage { sceneName = additiveScenes[0], sceneOperation = SceneOperation.LoadAdditive, customHandling = true });

        //    // We have Network Start Positions in first additive scene...pick one
        //    Transform start = StartPosition.positionAt(conn.identity.GetComponent<MultiplayerGamePlayer>().playerId);

        //    // Instantiate player as child of start position - this will place it in the additive scene
        //    // This also lets player object "inherit" pos and rot from start position transform
        //    GameObject player = Instantiate(playerPrefab, start);
        //    // now set parent null to get it out from under the Start Position object
        //    player.transform.SetParent(null);

        //    // Wait for end of frame before adding the player to ensure Scene Message goes first
        //    yield return new WaitForEndOfFrame();

        //    // Finally spawn the player object for this connection
        //    NetworkServer.AddPlayerForConnection(conn, player);
        //}

        public override void OnRoomStartHost()
        {
            base.OnRoomStartHost();

            networkDiscovery.AdvertiseServer();

            Debug.Log("OnRoomStartHost");
        }

        public override void OnRoomStopHost()
        {
            print("OnRoomStopHost");
            base.OnRoomStopHost();
            networkDiscovery.StopDiscovery();
        }

        public override void OnRoomStopClient()
        {
            print("OnRoomStopClient");
            base.OnRoomStopClient();
        }

        public override void OnRoomClientDisconnect()
        {
            print("OnRoomClientDisconnect");
            base.OnRoomClientDisconnect();

            // ����
            gameMap = null;
            selectedGameMap = null;
            if (isStartedGameplay)
            {
                isStartedGameplay = false;

                // for user business
                OnStopGameplay();

                // ж�ؽű�
                UnloadGameMap();

                Logger.Info("=========== stop Gameplay....");
            }
        }

        public override void OnRoomClientEnter()
        {
            base.OnRoomClientEnter();

            if (mode == NetworkManagerMode.ClientOnly)
            {
                networkDiscovery.StopDiscovery();
            }
        }


        /// <summary>
        /// This allows customization of the creation of the GamePlayer object on the server.
        /// <para>By default the gamePlayerPrefab is used to create the game-player, but this function allows that behaviour to be customized. The object returned from the function will be used to replace the room-player on the connection.</para>
        /// </summary>
        /// <param name="conn">The connection the player object is for.</param>
        /// <param name="roomPlayer">The room player object for this connection.</param>
        /// <returns>A new GamePlayer object.</returns>
        public override GameObject OnRoomServerCreateGamePlayer(NetworkConnectionToClient conn, GameObject roomPlayer)
        {
            // get start position from base class
            Transform startPos = StartPosition.positionAt(roomPlayer.GetComponent<MultiplayerGameRoomPlayer>().playerId);
            GameObject gamePlayer = startPos != null
                ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
                : Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

            return gamePlayer;
        }

        /// <summary>
        /// Called just after GamePlayer object is instantiated and just before it replaces RoomPlayer object.
        /// This is the ideal point to pass any data like player name, credentials, tokens, colors, etc.
        /// into the GamePlayer object as it is about to enter the Online scene.
        /// </summary>
        /// <param name="roomPlayer"></param>
        /// <param name="gamePlayer"></param>
        /// <returns>true unless some code in here decides it needs to abort the replacement</returns>
        public override bool OnRoomServerSceneLoadedForPlayer(NetworkConnectionToClient conn, GameObject roomPlayer, GameObject gamePlayer)
        {
            MultiplayerGamePlayer gplayer = gamePlayer.GetComponent<MultiplayerGamePlayer>();
            gplayer.playerId = roomPlayer.GetComponent<MultiplayerGameRoomPlayer>().playerId;
            gplayer.playerName = roomPlayer.GetComponent<MultiplayerGameRoomPlayer>().playerName;
            gplayer.playerRole = roomPlayer.GetComponent<MultiplayerGameRoomPlayer>().playerRole;
            return true;
        }

        /// <summary>
        /// ��ʶ�Ƿ�������Ѱ����
        /// </summary>
        public bool isDiscoveringServer { get; protected set; }
        /// <summary>
        /// �������ϲ�������
        /// </summary>
        public void StartDiscoveryServer()
        {
            isDiscoveringServer = true;
            networkDiscovery.StartDiscovery();
        }

        /// <summary>
        /// ֹͣ������������
        /// </summary>
        public void StopDiscoveryServer()
        {
            isDiscoveringServer = false;
            networkDiscovery.StopDiscovery();
        }


        public void OnDiscoveredServer(MultiplayerServerResponse info)
        {
            // Note that you can check the versioning to decide if you can connect to the server or not using this method
            discoveredServers_[info] = Time.realtimeSinceStartup;
        }

        public List<MultiplayerServerResponse> GetAllServers()
        {
            List<MultiplayerServerResponse> servers = new List<MultiplayerServerResponse>();
            foreach (var pair in discoveredServers_)
            {
                if (Time.realtimeSinceStartup - pair.Value < 3.0f)
                {
                    servers.Add(pair.Key);
                }
            }
            return servers;
        }

        #endregion

        #region GameMap

        /// <summary>
        /// ���ؽű�
        /// </summary>
        /// <param name="content"></param>
        /// <param name="onLoaded"></param>
        protected virtual void LoadMap(string content, System.Action onLoaded = null)
        {
            // loading page, hide when all player ready
            var loadingPage = uiRootWindow.GetChildWindow<LoadingScenePage>();
            loadingPage.Show();
            loadingPage.SetProgress(0);

            StartCoroutine(UpdateProgressBar(loadingPage));     // ���½���

            GameWorld.LoadMap(content, () =>
            {
                // attention: mirror
                FinishLoadScene();

                onLoaded?.Invoke();
            });
        }

        IEnumerator UpdateProgressBar(LoadingScenePage page)
        {
            bool bUpdateProgress = true;
            while (bUpdateProgress)
            {
                if (AssetBundleSceneManager.activeAsyncOperation != null)
                {
                    float p = AssetBundleSceneManager.activeAsyncOperation.progress;
                    page.SetProgress(p);

                    if (AssetBundleSceneManager.activeAsyncOperation.isDone || p > 0.9999999f)
                    {
                        bUpdateProgress = false;
                    }
                }

                yield return null;
            }
        }

        /// <summary>
        /// ж�ؽű���ͬʱ��ж�س�����
        /// </summary>
        public virtual void UnloadGameMap()
        {
            // ��������ϵͳ
            WeatherSystem.Clear();

            // reset
            GameWorld.UnloadGameMap();
        }

        /// <summary>
        /// ����ű���Ϣ
        /// </summary>
        /// <param name="message"></param>
        void OnReceiveGameMapInternal(GameMapMessage message)
        {
            Debug.Log("Receive game map:" + message.content);

            if (string.IsNullOrEmpty(message.content))
            {
                Wugou.Logger.Error("GameMapMessage error.");
                return;
            }

            GameMap map = new GameMap();
            map.Parse(message.content);
            if (string.IsNullOrEmpty(map.name))
            {
                Wugou.Logger.Error("GameMapMessage error.");
                return;
            }

            // ��ǰ��Ϸ��ͼ
            gameMap = map;
            if (mode == NetworkManagerMode.ClientOnly)
            {
                var page = uiRootWindow.GetChildWindow<InRoomPage>();
                page.SetGameMap(map);
            }
            OnGameMapChanged(map);
        }

        void OnStartGameInternal(StartGameMessage message)
        {
            LoadMap(gameMap.rawContent, () =>
            {
                // instantiate network gameobject
                if (mode == NetworkManagerMode.Host)
                {
                    Dictionary<string, GameObject> prefabDic = spawnPrefabs.ToDictionary(p => p.name);
                    for (int i = 0; i < GameWorld.gameEntities.Count; ++i)
                    {
                        var v = GameWorld.gameEntities[i];
                        if (prefabDic.ContainsKey(v.blueprint.prototype))
                        {
                            // TODO: instantiate on entity
                            GameObject go = Instantiate<GameObject>(prefabDic[v.blueprint.prototype]);
                            go.GetComponent<NetworkBindModel>().sceneObjectIndex = v.id;
                            NetworkServer.Spawn(go);    // ����ʵ����
                        }
                    }
                }

                NetworkClient.Send<LoadedGameSceneMessage>(new LoadedGameSceneMessage());
                uiRootWindow.GetChildWindow<LoadingScenePage>().SetText("�ȴ��������");

                OnStartGameplay();
                isStartedGameplay = true;

                // ������������
                uiRootWindow.GetChildWindow<LobbyPage>().Hide();
            });
        }

        public int loadedGameSceneCount { get; private set; } = 0;
        void OnLoadedGameSceneInternal(NetworkConnectionToClient conn, LoadedGameSceneMessage message)
        {
            loadedGameSceneCount++;
            if (loadedGameSceneCount == roomplayers.Count)
            {
                NetworkServer.SendToAll<ReadyGoMessage>(new ReadyGoMessage());
            }
        }

        void OnReadyGoInternal(ReadyGoMessage message)
        {
            uiRootWindow.GetChildWindow<LoadingScenePage>().Hide();
            MultiplayerGamePlayer.owner.ReadyGo();
        }

        #endregion

        #region MultiPlayer Gameplay

        public void Quit()
        {
            // ע���Ƴ�Manager
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

            // �ٴμӷ���Ϸ����
            SceneManager.LoadScene(GamePlay.kMainSceneName);
        }

        /// <summary>
        /// ��ǰServer��Hostѡ��Ľű�
        /// </summary>
        public GameMap selectedGameMap { get; set; }

        /// <summary>
        /// ���������뷿��
        /// </summary>
        public void CreateRoom(GameMap map)
        {
            selectedGameMap = map;

            // handle before create room
            OnCreateRoom();

            // TODO: fixed
            GameplayScene = GamePlay.kNetworkMainSceneName;

            StartHost();
        }

        /// <summary>
        /// ���뷿��
        /// </summary>
        /// <param name="room"></param>
        public void EnterRoom(System.Uri room)
        {
            StartClient(room);
        }

        public void ExitRoom()
        {
            if (mode == NetworkManagerMode.Host)
            {
                StopHost();
            }
            else
            {
                StopClient();
            }
        }

        public virtual void OnCreateRoom()
        {
            // 
        }

        /// <summary>
        /// �ű�����ʱ����
        /// </summary>
        /// <param name="map"></param>
        public virtual void OnGameMapChanged(GameMap map)
        {
            print("in network room");
        }

        /// <summary>
        /// ��ʼѵ��
        /// 1. �����������
        /// 2. ���س���;
        /// </summary>
        public void StartGameplay(GameMap map)
        {
            if (!(mode == NetworkManagerMode.Host || mode == NetworkManagerMode.ServerOnly))
            {
                return;
            }

            if (map == null)
            {
                Logger.Warning($"game map is null..");
                return;
            }

            // mirror
            networkSceneName = map.scene.sceneName;
            NetworkServer.SetAllClientsNotReady();

            //
            loadedGameSceneCount = 0;
            // Send Scene message to client to load the game scene
            NetworkServer.SendToAll(new StartGameMessage { });

            // stop discovery
            StopDiscoveryServer();
        }

        public void StopGameplay()
        {
            if (mode == NetworkManagerMode.Host)
            {
                StopHost();
            }
            else if (mode == NetworkManagerMode.ClientOnly)
            {
                StopClient();
            }
        }

        /// <summary>
        /// ����������Ϸ�ű����ڿͻ��˵���,���紴����Ϸ��¼
        /// </summary>
        protected virtual void OnStartGameplay()
        {

        }

        /// <summary>
        /// ������Ϸʱ�ڿͻ��˵���,������²�������Ϸ��¼
        /// </summary>
        protected virtual void OnStopGameplay()
        {

        }

        /// <summary>
        /// ��¼���뷿������ 
        /// </summary>
        /// <param name="roomPlayer"></param>
        public void ReportRoomPlayer(MultiplayerGameRoomPlayer roomPlayer)
        {
            roomplayers[roomPlayer.playerId] = roomPlayer;
        }

        /// <summary>
        /// ��¼������Ϸ�����
        /// </summary>
        public void ReportGamePlayer(MultiplayerGamePlayer player)
        {
            gameplayers[player.playerId] = player;
        }

        /// <summary>
        /// ��ȡ���ʵ��
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public MultiplayerGamePlayer GetGameplayer(int id)
        {
            if (gameplayers.ContainsKey(id))
            {
                return gameplayers[id];
            }

            return null;
        }

        /// <summary>
        /// ������ҿ�����Ϣ���������ڽ����¼
        /// </summary>
        public virtual void UpdateGameplayerSnapshot(MultiplayerGamePlayer player)
        {

        }

        #endregion

    }
}