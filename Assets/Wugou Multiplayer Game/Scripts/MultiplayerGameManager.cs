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
using Wugou.Examples;

namespace Wugou.Multiplayer
{
    /// <summary>
    /// ����Mirror�Ķ���ѵ��ϵͳ
    /// </summary>
    public class MultiplayerGameManager : NetworkRoomManager
    {
        public static MultiplayerGameManager instance => (NetworkRoomManager.singleton as MultiplayerGameManager);

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

        /// <summary>
        /// ��Ϸ��ʼʱ�䣬���ڼ�ʱ
        /// </summary>
        public double gameStartTime { get; protected set; }

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
            public double startTime;
        }

        public override void OnRoomStartServer()
        {
            print("OnRoomStartServer");
            base.OnRoomStartServer();

            // �������Ϣ����
            NetworkServer.RegisterHandler<LoadedGameSceneMessage>(OnLoadedGameSceneInternal, false);

        }


        public override void OnRoomStartClient()
        {
            base.OnRoomStartClient();

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

            AdvertiseServer();

            Debug.Log("OnRoomStartHost");
        }

        public override void OnRoomStopHost()
        {
            print("OnRoomStopHost");
            base.OnRoomStopHost();
            StopServerDiscovery();
        }

        public override void OnRoomStopClient()
        {
            print("OnRoomStopClient");
            base.OnRoomStopClient();
        }

        public override void OnRoomClientDisconnect()
        {
            // ���ش󳡾�ʱ����ʱ
            if(GameWorld.isLoading)
            {
                GameWorld.interruptLoading = true;

                //uiRootWindow.GetChildWindow<MakeSurePage>().ShowTips("���س�����ʱ!", () =>
                //{

                //});
            }

            print("OnRoomClientDisconnect");
            base.OnRoomClientDisconnect();

            // ����
            gameMap = null;
            selectedGameMap = null;
            if (isStartedGameplay)
            {
                isStartedGameplay = false;

                // ж�ؽű�
                UnloadGameMap();

                // for user business
                OnStopGameplay();

                Logger.Info("=========== stop Gameplay....");
            }
        }

        public override void OnRoomClientEnter()
        {
            base.OnRoomClientEnter();

            if (mode == NetworkManagerMode.ClientOnly)
            {
                StopServerDiscovery();
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

            // ��UIˢ�£���ΪUnity�������첽���أ�Ҳ���ܿ������߳�
            StartCoroutine(WaitLoadMap(content, onLoaded));
        }

        /// <summary>
        /// Unity3D���س����Ῠ�٣��������첽���أ��󳡾��������ʱ�������UI������
        /// </summary>
        /// <param name="content"></param>
        /// <param name="onLoaded"></param>
        /// <returns></returns>
        IEnumerator WaitLoadMap(string content, System.Action onLoaded)
        {
            if(mode == NetworkManagerMode.Host)
            {
                // ��������һ�ȣ���������Ϣ������ȥ������Ӧ�������أ�����ͻ����ղ�����Ϣ���Ῠ�ڷ���ҳ��
                // ��Ȼ����̫���գ����ǻ����˺ܶ�,��û���unity�𿨶٣�������˵mirrorӦ���������̷߳��͵���Ϣ��������Unity���߳�Ӱ�죬Ӧ�õ�һ֡�Ϳ����ˡ�����
                yield return null;
                yield return null;
                yield return null;
            }

            GameWorld.LoadMap(content, () =>
            {
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
            OnGameMapChanged(map);
        }

        /// <summary>
        /// ���ڱ�ʶreadygo��Ϣ�Ƿ���
        /// </summary>
        private bool isSendReadyGo { get; set; } = false;
        void OnStartGameInternal(StartGameMessage message)
        {
            // set server flag to stop processing messages while changing scenes
            // it will be re-enabled in FinishLoadScene.
            NetworkServer.isLoadingScene = true;

            LoadMap(gameMap.rawContent, () =>
            {
                // attention: mirror
                FinishLoadScene();

                if (mode == NetworkManagerMode.Host)
                {
                    // ��������ͬ������Ķ������⴦��
                    for(int i=0;i<GameWorld.gameEntities.Count;i++)
                    {
                        var dd = GameWorld.gameEntities[i];
                        var comp = GameWorld.gameEntities[i].GetComponent<GameEntityNetworkify>();
                        if (comp != null)
                        {
                            comp.NetworkInstantiate();
                        }
                    }
                }

                NetworkClient.Send<LoadedGameSceneMessage>(new LoadedGameSceneMessage());
                uiRootWindow.GetChildWindow<LoadingScenePage>().SetProgress(1.0f);
                uiRootWindow.GetChildWindow<LoadingScenePage>().SetText("�ȴ��������");
                if(mode == NetworkManagerMode.Host)
                {
                    // ���ȴ�20��
                    StartCoroutine(WaitingSetTimeAndStart(25));
                }

                OnStartGameplay();
                isStartedGameplay = true;
            });
        }

        protected IEnumerator WaitingSetTimeAndStart(float time)
        {
            yield return new WaitForSeconds(time);

            if (!isSendReadyGo)
            {
                isSendReadyGo = true;
                NetworkServer.SendToAll<ReadyGoMessage>(new ReadyGoMessage() { startTime = NetworkTime.time});
            }

        }

        public int loadedGameSceneCount { get; private set; } = 0;
        void OnLoadedGameSceneInternal(NetworkConnectionToClient conn, LoadedGameSceneMessage message)
        {
            loadedGameSceneCount++;
            if (!isSendReadyGo && loadedGameSceneCount == roomplayers.Count)
            {
                isSendReadyGo = true;
                NetworkServer.SendToAll<ReadyGoMessage>(new ReadyGoMessage() { startTime = NetworkTime.time });
            }
        }

        void OnReadyGoInternal(ReadyGoMessage message)
        {
            gameStartTime = message.startTime;

            // ��Ҫ��Գ�ʱ��������������������ȴ�����ʱ��Ϳ�ʼ��Ϸ�ˣ����е���һ�δ�������
            StartCoroutine(ReadyGoCoroutine());
        }

        IEnumerator ReadyGoCoroutine()
        {
            //int count = 120;     // �ٵȴ�һ��ʱ��
            //while(count-- > 0)
            //{
            //    // �����г�ʱ�����
            //    if (MultiplayerGamePlayer.owner)
            //    {
            //        uiRootWindow.GetChildWindow<LoadingScenePage>().Hide();
            //        MultiplayerGamePlayer.owner.ReadyGo();
            //        break;
            //    }

            //    yield return new WaitForSeconds(0.5f);
            //}

            //// ����
            //if(!MultiplayerGamePlayer.owner)
            //{
            //    StopGameplay();
            //}

            while (!MultiplayerGamePlayer.owner)
            {
                yield return new WaitForSeconds(0.5f);
            }

            uiRootWindow.GetChildWindow<LoadingScenePage>().SetProgress(1.0f);
            yield return new WaitForSeconds(0.2f);  // ������90%ͻȻ���볡���е�ͻأ
            uiRootWindow.GetChildWindow<LoadingScenePage>().Hide();
            MultiplayerGamePlayer.owner.ReadyGo();

        }

        #endregion

        #region MultiPlayer Gameplay

        public void Quit()
        {
            // ע���Ƴ�Manager
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

            //
            if(mode != NetworkManagerMode.Offline)
            {
                ExitRoom();
            }

            // �ٴμӷ���Ϸ����
            SceneManager.LoadScene(GamePlay.settings.mainSceneName);
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
            GameplayScene = GamePlay.settings.networkMainSceneName;

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
            isSendReadyGo = false;
            // Send Scene message to client to load the game scene
            NetworkServer.SendToAll(new StartGameMessage { });

            // stop discovery
            StopServerDiscovery();
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
            // �ռ������Ϣ����¼
            foreach(var v in gameplayers)
            {
                UpdateGameplayerSnapshot(v.Value);
            }

            var gameStats = GamePlay.lastGameStats;
            gameStats.duration = Time.realtimeSinceStartup - gameStats.duration;

            // д��¼
            GamePlay.gameStatsManager.AddGameStats(gameStats);

        }

        /// <summary>
        /// ��¼���뷿������ 
        /// </summary>
        /// <param name="roomPlayer"></param>
        public void AddRoomPlayer(MultiplayerGameRoomPlayer roomPlayer)
        {
            roomplayers[roomPlayer.playerId] = roomPlayer;
        }

        /// <summary>
        /// ����ɾ�����ߵ����
        /// </summary>
        /// <param name="roomPlayer"></param>
        public void RemoveRoomPlayer(MultiplayerGameRoomPlayer roomPlayer)
        {
            if (roomplayers.ContainsKey(roomPlayer.playerId))
            {
                roomplayers.Remove(roomPlayer.playerId);
            }
        }

        /// <summary>
        /// ��¼������Ϸ�����
        /// </summary>
        public void AddGamePlayer(MultiplayerGamePlayer player)
        {
            gameplayers[player.playerId] = player;
        }

        /// <summary>
        /// ����ɾ�����ߵ����
        /// </summary>
        /// <param name="player"></param>
        public void RemoveGamePlayer(MultiplayerGamePlayer player)
        {
            if (gameplayers.ContainsKey(player.playerId))
            {
                gameplayers.Remove(player.playerId);
            }
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

            Wugou.Logger.Error($"Player id {id} not exist. Maybe not login or connect timeout(auto disconnect).");
            return null;
        }

        /// <summary>
        /// ������ҿ�����Ϣ���������ڽ����¼
        /// </summary>
        public virtual void UpdateGameplayerSnapshot(MultiplayerGamePlayer player)
        {

        }

        /// <summary>
        /// Ϊ��ҷ���ID
        /// </summary>
        /// <returns></returns>
        public int AllocatePlayerID()
        {
            // id ����
            int id = 0;
            var players = MultiplayerGameManager.instance.roomplayers;
            HashSet<int> ids = new HashSet<int>();
            foreach (var p in players.Values)
            {
                if (p && p.playerId >= 0)
                {
                    ids.Add(p.playerId);
                }
            };

            // Ѱ����С�Ŀ���id�������ڷ����е�����б�
            while (ids.Contains(id))
            {
                ++id;
            }

            return id;
        }

        #endregion

        #region ���ҷ�����

        public bool isDiscoveringServer { get; private set; } = false;
        public virtual void AdvertiseServer()
        {
            GetComponent<MultiplayerNetworkDiscovery>().AdvertiseServer();
        }

        public virtual void StartServerDiscovery()
        {
            GetComponent<MultiplayerNetworkDiscovery>().StartDiscovery();
            isDiscoveringServer = true;
        }

        public virtual void StopServerDiscovery()
        {
            GetComponent<MultiplayerNetworkDiscovery>().StopDiscovery();
            isDiscoveringServer = false;
        }

        #endregion
    }
}