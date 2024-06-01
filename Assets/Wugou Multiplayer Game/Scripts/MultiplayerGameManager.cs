using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.SceneManagement;
using Wugou.UI;
using Mirror;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Wugou.Multiplayer
{
    /// <summary>
    /// 基于Mirror的多人训练系统
    /// </summary>
    public class MultiplayerGameManager : NetworkRoomManager
    {
        public static MultiplayerGameManager instance => (NetworkRoomManager.singleton as MultiplayerGameManager);

        /// <summary>
        /// 标识是否正在游戏
        /// </summary>
        public bool isStartedGameplay { get; protected set; }

        /// <summary>
        /// 游戏开始时间，用于计时
        /// </summary>
        public double gameStartTime { get; protected set; }

        /// <summary>
        /// server信息
        /// </summary>
        public MultiplayerServerResponse serverResponse { get; private set; }

        #region UnityFunctions

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (isStartedGameplay)
            {
                StopGameplay();
            }
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
        /// 服务端在新玩家进入时发送，脚本消息，用于同步加载脚本
        /// </summary>
        public struct GameMapPackageMessage : NetworkMessage
        {
            public string name;
            public byte[] content;
        }

        /// <summary>
        /// 服务端点击开始游戏时发送
        /// </summary>
        public struct StartGameMessage : NetworkMessage { }

        /// <summary>
        /// 客户端加载完游戏场景时发送 
        /// </summary>
        public struct LoadedGameSceneMessage : NetworkMessage
        {
        }

        /// <summary>
        /// 服务器客户端可以开始游戏了
        /// </summary>
        public struct ReadyGoMessage : NetworkMessage
        {
            public double startTime;
        }

        public override void OnRoomStartServer()
        {
            Logger.DebugInfo("OnRoomStartServer");
            base.OnRoomStartServer();

            // 服务端消息处理
            NetworkServer.RegisterHandler<LoadedGameSceneMessage>(OnLoadedGameSceneInternal, false);
        }


        public override void OnRoomStartClient()
        {
            base.OnRoomStartClient();

            // 客户端消息处理 
            NetworkClient.RegisterHandler<GameMapPackageMessage>(OnReceiveGameMapInternal, false);
            NetworkClient.RegisterHandler<StartGameMessage>(OnStartGameInternal, false);
            NetworkClient.RegisterHandler<ReadyGoMessage>(OnReadyGoInternal, false);

            Logger.DebugInfo("OnRoomStartClient");
        }

        public override void OnRoomClientConnect()
        {
            base.OnRoomClientConnect();

            Logger.DebugInfo("OnRoomClientConnect");
        }


        /// <summary>
        /// Called on the server when a client adds a new player with NetworkClient.AddPlayer.
        /// <para>The default implementation for this function creates a new player object from the playerPrefab.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            Logger.DebugInfo("OnServerAddPlayer");
            base.OnServerAddPlayer(conn);
        }

        //public override GameObject OnRoomServerCreateRoomPlayer(NetworkConnectionToClient conn)
        //{
        //    // 发送GameMap给客户端
        //    conn.Send<GameMapPackageMessage>(new GameMapPackageMessage { name = Path.GetFileName(hostGameMapPackage.path), content = File.ReadAllBytes(hostGameMapPackage.path) });

        //    return base.OnRoomServerCreateRoomPlayer(conn);
        //}

        /// <summary>
        /// Called on the server when a client is ready.
        /// <para>The default implementation of this function calls NetworkServer.SetClientReady() to continue the network setup process.</para>
        /// </summary>
        /// <param name="conn">Connection from client.</param>
        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            Logger.DebugInfo("OnServerReady");
            // This fires from a Ready message client sends to server after loading the online scene
            base.OnServerReady(conn);

            //if (conn.identity == null)
            //{
            //    StartCoroutine(AddPlayerDelayed(conn));   // 
            //}
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

            //
            gameMapSync.StartServer();

            Debug.Log("OnRoomStartHost");
        }

        public override void OnRoomStopHost()
        {
            Logger.DebugInfo("OnRoomStopHost");
            base.OnRoomStopHost();

            StopServerDiscovery();

            gameMapSync.Shutdown();
        }

        public override void OnRoomStopClient()
        {
            Logger.DebugInfo("OnRoomStopClient");
            base.OnRoomStopClient();
        }

        public override void OnRoomClientDisconnect()
        {
            // 加载大场景时，超时
            if(GameWorld.isLoading)
            {
                GameWorld.interruptLoading = true;

                //uiRootWindow.GetChildWindow<MakeSurePage>().ShowTips("加载场景超时!", () =>
                //{

                //});
            }

            Logger.DebugInfo("OnRoomClientDisconnect");
            base.OnRoomClientDisconnect();

            // 确保一下
            gameMapSync.Shutdown();

            // 结束
            gameMapPackage = null;

            if (isStartedGameplay)
            {
                isStartedGameplay = false;
                Gameplay.isGaming = false;

                // 
                OnStopGameplay();

                Logger.Info("=========== stop Gameplay....");
            }

            // 卸载脚本
            UnloadGameMap();

            // hide daemon ui
            DaemonUI.HideAllWindow();
            // UI清理
            UIRootWindow.Release();
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
            Transform startPos = StartPosition.positionAt(roomPlayer.GetComponent<MultiplayerRoomPlayer>().playerId);
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
            gplayer.playerId = roomPlayer.GetComponent<MultiplayerRoomPlayer>().playerId;
            gplayer.playerName = roomPlayer.GetComponent<MultiplayerRoomPlayer>().playerName;
            gplayer.playerRole = roomPlayer.GetComponent<MultiplayerRoomPlayer>().playerRole;
            return true;
        }

        #endregion

        #region GameMap

        /// <summary>
        /// 加载脚本
        /// </summary>
        /// <param name="packagePath"></param>
        protected virtual async Task<bool> LoadGameMapPackage(string packagePath)
        {
            // loading page, hide when all player ready
            DaemonUI.loadingPage.Show();
            DaemonUI.loadingPage.SetProgress(0.1f);
            DaemonUI.loadingPage.UpdateProgressBar(() => {
                if (GameWorld.loadingSceneOperation != null)
                {
                    if (GameWorld.loadingSceneOperation.isDone)
                    {
                        return 1.0f;
                    }

                    return 0.1f + GameWorld.loadingSceneOperation.progress * 0.9f;
                }

                return DaemonUI.loadingPage.GetProgress();
            });     // 更新进度

            // 等UI刷新，因为Unity哪怕是异步加载，也可能卡死主线程
            if (mode == NetworkManagerMode.Host)
            {
                // 服务器等一等，等网络消息都发出去了再响应场景加载，否则客户端收不到消息，会卡在房间页面
                // 虽然不是太保险，但是缓解了很多,最好还是unity别卡顿，按理来说mirror应该在其他线程发送的消息，不会受Unity主线程影响，应该等一帧就可以了。。。
                await new YieldInstructionAwaiter(null);
                await new YieldInstructionAwaiter(null);
                await new YieldInstructionAwaiter(null);
            }

            if (string.IsNullOrEmpty(packagePath))
            {
                Logger.Error($"Empty GameMap....");
                return false;
            }

            // set server flag to stop processing messages while changing scenes
            // it will be re-enabled in FinishLoadScene.
            NetworkServer.isLoadingScene = true;

            var succ = await GameWorld.LoadGameMapPackage(packagePath, LoadSceneMode.Single);
            if (succ == GameWorld.Error.kSuccess)
            {

                // 模拟Mirror中对Scene GameObject的处理，参照NetworkIdentity.SetSceneIdSceneHashPartInternal
                for (int i = 0; i < GameWorld.gameEntities.Count; i++)
                {
                    var entity = GameWorld.gameEntities[i];
                    var identity = entity.GetComponent<NetworkIdentity>();
                    if (identity)
                    {
                        identity.sceneId = GetSceneId(entity.id);
                        identity.gameObject.SetActive(false);
                    }

                    // PlainTextComponent as serialize text, deserialize
                    var comp = entity.GetComponent<PlainTextComponent>();
                    if (!comp)
                    {
                        continue;
                    }
                    try
                    {
                        var jo = JObject.Parse(comp.text);

                        if (jo["type"] != null)
                        {
                            var compType = ReflectType(jo["type"].ToString());
                            if (compType != null)
                            {
                                var addedComp = comp.gameObject.AddComponent(compType);
                                var content = jo["content"].ToString();
                                if (!string.IsNullOrEmpty(content))
                                {
                                    Newtonsoft.Json.JsonConvert.PopulateObject(content, (object)addedComp);
                                }

                                // 添加必要的NetworkIdentity
                                if(addedComp is NetworkBehaviour && !entity.GetComponent<NetworkIdentity>())
                                {
                                    var networkId = entity.gameObject.AddComponent<NetworkIdentity>();
                                    networkId.sceneId = GetSceneId(entity.id);
                                    entity.gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        // 
                        Logger.Error($"{comp.name}'PlainTextComponent content parse error.{ex.Message}");
                    }
                }

                // attention: mirror for trigger OnServerSceneChange or OnClientSceneChange
                FinishLoadScene();

                // 发送加载完成消息，所有人加载完成后由服务端发送开始消息
                NetworkClient.Send(new LoadedGameSceneMessage());

                DaemonUI.loadingPage.SetProgress(1.0f);
                DaemonUI.loadingPage.SetText("等待其他玩家");

                // 注意是异步
                Utils.DoAsync(async () =>
                {
                    if (mode == NetworkManagerMode.Host)
                    {
                        // 等待一段时间
                        await new YieldInstructionAwaiter(new WaitForSeconds(30));
                        if (!isSendReadyGoMessage)
                        {
                            SendReadyGoMessageToAll();
                        }
                    }
                });

                return true;
            }
            else
            {
                StopGameplay();

                return false;
            }
        }

        /// <summary>
        /// 用于PlainTextComponent反射的类型
        /// </summary>
        protected static Dictionary<string, Type> plainTextComponentTypes = new Dictionary<string,Type>();

        /// <summary>
        /// 反射获取类型，主要用于获取业务程序集的类型
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static Type ReflectType(string name)
        {
            if (plainTextComponentTypes.ContainsKey(name))
            {
                return plainTextComponentTypes[name];
            }

            var type = Type.GetType(name);   //先获取当前程序集的
            if (type != null)
            {
                return type;
            }
            return System.Type.GetType($"{name}, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
        }

        /// <summary>
        /// 根据entity id 生成Mirror的NetworkIdentity需要的sceneId
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        private ulong GetSceneId(int entityId)
        {
            // 使用GameEntity的id作为SceneID
            var sceneId = (ulong)entityId;

            string scenePath = GameWorld.activeScene.name.ToLower();

            // get deterministic scene hash
            uint pathHash = (uint)scenePath.GetStableHashCode();

            // shift hash from 0x000000FFFFFFFF to 0xFFFFFFFF00000000
            ulong shiftedHash = (ulong)pathHash << 32;

            // OR into scene id
            sceneId = (sceneId & 0xFFFFFFFF) | shiftedHash;

            return sceneId;
        }


        /// <summary>
        /// 卸载脚本（同时会卸载场景）
        /// </summary>
        public virtual void UnloadGameMap()
        {
            // reset
            GameWorld.UnloadGameMap();
        }

        /// <summary>
        /// 处理脚本消息
        /// </summary>
        /// <param name="message"></param>
        void OnReceiveGameMapInternal(GameMapPackageMessage message)
        {
            Logger.DebugInfo("Receive game map:" + message.content.Length);

            if (string.IsNullOrEmpty(message.name))
            {
                return;
            }

            // 客户端写入
            //if (mode == NetworkManagerMode.ClientOnly)   
            //{
            //    var dst = $"{Gameplay.downloadGameMapsPath}/{message.name}";
            //    Directory.CreateDirectory(Gameplay.downloadGameMapsPath);
            //    File.WriteAllBytes(dst, message.content);

            //    // 当前游戏地图
            //    gameMapPackagePath = dst;
            //}
            //else
            //{
            //    // 当前游戏地图
            //    gameMapPackagePath = hostGameMapPackage.path;
            //}

            OnGameMapChanged(gameMapPackage.name);
        }

        async void OnStartGameInternal(StartGameMessage message)
        {
            // 
            gameMapSync.Shutdown();

            Gameplay.isGaming = true;
            var succ = await LoadGameMapPackage(gameMapPackage.path);
            if (succ)
            {
                isStartedGameplay = true;
            }
        }

        /// <summary>
        /// 用于标识readygo消息是否发送
        /// </summary>
        private bool isSendReadyGoMessage { get; set; } = false;

        private void SendReadyGoMessageToAll()
        {
            isSendReadyGoMessage = true;
            gameStartTime = NetworkTime.time;
            NetworkServer.SendToAll(new ReadyGoMessage() { startTime = gameStartTime });
        }

        public int loadedGameSceneCount { get; private set; } = 0;
        void OnLoadedGameSceneInternal(NetworkConnectionToClient conn, LoadedGameSceneMessage message)
        {
            loadedGameSceneCount++;
            if (!isSendReadyGoMessage)
            {
                // 所有人都正常加载
                if(loadedGameSceneCount == MultiplayerRoomPlayer.allPlayers.Count)
                {
                    SendReadyGoMessageToAll();
                }
            }
            else
            {
                // 有人超时了
                conn.Send(new ReadyGoMessage() { startTime = gameStartTime });
            }
        }

        void OnReadyGoInternal(ReadyGoMessage message)
        {
            gameStartTime = message.startTime;

            // 
            OnStartGameplay();

            // 主要针对超时的情况，即服务器就绪等待到超时后就开始游戏了，但有的玩家还未加载完成
            StartCoroutine(ReadyGoCoroutine());
        }

        IEnumerator ReadyGoCoroutine()
        {
            // 等待自己角色的实例化
            int times = 60;
            while (!MultiplayerGamePlayer.owner && times-- > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            if(MultiplayerGamePlayer.owner)
            {
                DaemonUI.loadingPage.SetProgress(1.0f);
                yield return new WaitForSeconds(0.2f);  // 进度条90%突然进入场景有点突兀
                DaemonUI.loadingPage.Hide();
            }
            else
            {
                Logger.Error("MultiplayerGamePlayer.owner is null....");
                StopGameplay();
                DaemonUI.makeSurePage.Tips("超时退出！");
            }
        }

        #endregion

        #region MultiPlayer Gameplay

        public void Quit()
        {
            // 注意移除Manager
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

            //
            if(mode != NetworkManagerMode.Offline)
            {
                ExitRoom();
            }

            // 再次加非游戏场景
            SceneManager.LoadScene(Gameplay.settings.networkMainScene);
        }

        /// <summary>
        /// 当前Server或Host选择的脚本
        /// </summary>
        public GameMapPackage gameMapPackage { get; set; }

        /// <summary>
        /// 用于从服务器下载游戏脚本
        /// </summary>
        public GameMapSync gameMapSync { get; private set; } = new GameMapSync();

        /// <summary>
        /// 创建房间
        /// </summary>
        /// <param name="package"></param>
        public bool CreateRoom(GameMapPackage package)
        {
            gameMapPackage = package;

            try
            {
                StartHost();

                return true;
            }
            catch (Exception e)
            {
                DaemonUI.makeSurePage.Tips($"创建房间失败！{e.Message}");

                return false;
            }
        }

        /// <summary>
        /// 加入房间
        /// </summary>
        /// <param name="room"></param>
        public void EnterRoom(MultiplayerServerResponse response)
        {
            serverResponse = response;

            long ts = -1;
            long.TryParse(response.gameMapMd5, out ts);
            // 查找本地是否有地图
            var packages = Gameplay.gameMapManager.GetAllNames();
            for(int i=0;i<packages.Count;i++)
            {
                int pos = packages[i].LastIndexOf('/');
                var mapName = packages[i].Substring(pos+1);
                if (mapName == response.gameMapPackage)
                {
                    var package = Gameplay.gameMapManager.Get(packages[i]);
                    if(package.gameMap.timestamp == ts)
                    {
                        gameMapPackage = package;
                        break;
                    }
                }
            }

            // 没找到，从服务器下载
            // 注意，如果存在同名但md5不一样的GameMap，则旧的会被覆盖
            if(gameMapPackage == null)
            {
                //
                gameMapSync.StartClient(response.uri, (mapFile) =>
                {
                    gameMapPackage = new GameMapPackage(mapFile);
                });
            }

            StartClient(response.uri);
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

        /// <summary>
        /// 脚本更换时调用
        /// </summary>
        /// <param name="name"></param>
        public virtual void OnGameMapChanged(string name)
        {
            Logger.Info($"Change GameMap to {name}...");
        }

        /// <summary>
        /// 预解压GameMap
        /// </summary>
        /// <param name="path"></param>
        public void PreExtractGameMap(string path)
        {
            var mapPath = path;
            var cachePath = $"{Gameplay.gameMapCachePath}";
            string packageName = Path.GetFileNameWithoutExtension(mapPath);
            GameMapPackage.Extract($"{mapPath}", cachePath);

            var packageDir = $"{cachePath}/{packageName}";
            if (!Directory.Exists(packageDir))
            {
                Wugou.Logger.Error($"Extract {mapPath} to {cachePath} fail...");
            }
        }

        /// <summary>
        /// 开始训练
        /// 1. 开启网络服务；
        /// 2. 加载场景;
        /// 
        /// 流程：
        /// StartGameplay
        ///    Load GameScene
        ///        Wait others loaded Scene
        ///             Wait ReadyGo
        /// 用于客户端处理正式开始游戏的逻辑，客户端加载完场景后调用
        /// </summary>
        public void StartGameplay(GameMapPackage package)
        {
            if (!(mode == NetworkManagerMode.Host || mode == NetworkManagerMode.ServerOnly))
            {
                return;
            }

            if (package == null)
            {
                Logger.Warning($"game map is null..");
                return;
            }

            // 
            StopServerDiscovery();

            // mirror
            networkSceneName = package.gameMap.scene;
            NetworkServer.SetAllClientsNotReady();

            //
            loadedGameSceneCount = 0;
            isSendReadyGoMessage = false;
            // Send Scene message to client to load the game scene
            NetworkServer.SendToAll(new StartGameMessage { });
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
            else
            {
                Logger.Error($"{mode} not within expectations");
            }
        }

        /// <summary>
        /// 在所有的客户端调用
        /// 当加载完游戏脚本后在客户端调用,比如创建游戏记录
        /// </summary>
        protected virtual void OnStartGameplay()
        {
        }

        /// <summary>
        /// 结束游戏时在客户端调用,比如更新并保存游戏记录
        /// </summary>
        protected virtual void OnStopGameplay()
        {
            // 收集玩家信息，记录
            foreach(var v in MultiplayerGamePlayer.allPlayers)
            {
                UpdateGameplayerSnapshot(v.Value);
            }

            SaveGameStat();
        }

        protected virtual void SaveGameStat()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 更新玩家快照信息，比如用于结果记录
        /// </summary>
        public virtual void UpdateGameplayerSnapshot(MultiplayerGamePlayer player)
        {

        }

        private static int sPlayerIdIndex = 0;      // 用于分配player id
        public int AllocatePlayerID()
        {
            return sPlayerIdIndex++;
        }

        /// <summary>
        /// 为玩家分配房间中的位置, 只在服务端运行
        /// </summary>
        /// <returns></returns>
        public int AllocateRoomSeat()
        {
            // 查找最小的可用的位置
            HashSet<int> ids = new HashSet<int>();
            foreach (var p in MultiplayerRoomPlayer.allPlayers.Values)
            {
                if (p && p.roomSeat >= 0)
                {
                    ids.Add(p.roomSeat);
                }
            };

            int id = 0;
            // 寻找最小的可用id，用于在房间中的玩家列表
            while (ids.Contains(id))
            {
                ++id;
            }

            return id;
        }

        #endregion

        #region 查找服务器

        public bool isDiscoveringServer { get; private set; } = false;
        public virtual void AdvertiseServer()
        {
            GetComponent<MultiplayerNetworkDiscovery>()?.AdvertiseServer();
        }

        public virtual void StartServerDiscovery()
        {
            GetComponent<MultiplayerNetworkDiscovery>()?.StartDiscovery();
            isDiscoveringServer = true;
        }

        public virtual void StopServerDiscovery()
        {
            GetComponent<MultiplayerNetworkDiscovery>()?.StopDiscovery();
            isDiscoveringServer = false;
        }

        #endregion
    }

    /// <summary>
    /// 用于同步服务器和客户端间的游戏脚本
    /// 1.借鉴Mirror discovery server代码, 实现客户端向服务器下载脚本，注意NetworkReaderPooled中的Reader的实现，如果不继承NetworkMessage，则需要自定义reader；
    /// 2.使用UDP的方案最终放弃了，因为下载的脚本带资源，可能很大，这就需要切片，感觉麻烦；
    /// 3.同一个思路改为使用tcp，不想那么复杂了
    /// </summary>
    public class GameMapSync
    {  
        private Thread serverThread_ = null;
        private Thread clientThread_ = null;

        private int gameMapServerPort = 49999;

        public void StartServer()
        {
            Shutdown();

            // Setup port -- may throw exception

            serverThread_ = new Thread(async () =>
            {
                TcpListener tcpListener = null;
                try
                {
                    tcpListener = new TcpListener(IPAddress.Any, gameMapServerPort);
                    tcpListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    tcpListener.Start();

                    while (MultiplayerGameManager.instance)
                    {
                        TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                        if (tcpClient.Connected && MultiplayerGameManager.instance)
                        {
                            NetworkStream stream = tcpClient.GetStream();

                            string fullPath = MultiplayerGameManager.instance.gameMapPackage.path;
                            string fileName = Path.GetFileName(fullPath);
                            byte[] fileNameByte = Encoding.Unicode.GetBytes(fileName);
                            byte[] fileNameLengthForValueByte = Encoding.Unicode.GetBytes(fileNameByte.Length.ToString("D11"));
                            byte[] fileAttributeByte = new byte[fileNameByte.Length + fileNameLengthForValueByte.Length];

                            fileNameLengthForValueByte.CopyTo(fileAttributeByte, 0);  //文件名字符流的长度的字符流排在前面。
                            fileNameByte.CopyTo(fileAttributeByte, fileNameLengthForValueByte.Length);  //紧接着文件名的字符流

                            stream.Write(fileAttributeByte, 0, fileAttributeByte.Length);

                            using (var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                            {
                                int fileReadSize = 0;
                                long fileLength = 0;
                                while (fileLength < fileStream.Length)
                                {
                                    byte[] buffer = new byte[2048];
                                    fileReadSize = fileStream.Read(buffer, 0, buffer.Length);
                                    stream.Write(buffer, 0, fileReadSize);
                                    fileLength += fileReadSize;

                                }
                                fileStream.Flush();
                            }
                            stream.Flush();
                            stream.Close();
                        }
                    }

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
                finally
                {
                    tcpListener?.Stop();
                }

            });
            serverThread_.IsBackground = true;
            serverThread_.Start();
        }

        /// <summary>
        /// Start Active Discovery
        /// </summary>
        public void StartClient(Uri server, System.Action<string> onReceive)
        {
            Shutdown();

            try
            {
                // Setup port
                clientThread_ = new Thread(() =>
                {
                    using (TcpClient tcpClient = new TcpClient())
                    {
                        try
                        {
                            tcpClient.Connect(server.Host, gameMapServerPort);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);

                        }

                        if (tcpClient.Connected)
                        {
                            NetworkStream stream = tcpClient.GetStream();
                            if (stream != null)
                            {

                                byte[] fileNameLengthForValueByte = Encoding.Unicode.GetBytes((256).ToString("D11"));
                                byte[] fileNameLengByte = new byte[1024];
                                int fileNameLengthSize = stream.Read(fileNameLengByte, 0, fileNameLengthForValueByte.Length);
                                string fileNameLength = Encoding.Unicode.GetString(fileNameLengByte, 0, fileNameLengthSize);
                                Logger.DebugInfo("文件名字符流的长度为：" + fileNameLength);

                                int fileNameLengthNum = Convert.ToInt32(fileNameLength);
                                byte[] fileNameByte = new byte[fileNameLengthNum];

                                int fileNameSize = stream.Read(fileNameByte, 0, fileNameLengthNum);
                                string fileName = Encoding.Unicode.GetString(fileNameByte, 0, fileNameSize);
                                Logger.DebugInfo("文件名为：" + fileName);

                                string dirPath = Gameplay.downloadGameMapsPath;
                                string rcvFile = dirPath + "/" + fileName;
                                using (FileStream fileStream = new FileStream(rcvFile, FileMode.Create, FileAccess.Write))
                                {
                                    int fileReadSize = 0;
                                    byte[] buffer = new byte[2048];
                                    while ((fileReadSize = stream.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        fileStream.Write(buffer, 0, fileReadSize);
                                    }

                                    Logger.DebugInfo("接收成功");

                                    onReceive?.Invoke(rcvFile);
                                }
                                stream.Flush();
                                stream.Close();
                            }
                        }
                    }

                });
                clientThread_.IsBackground = true;
                clientThread_.Start();

            }
            catch (Exception)
            {
                // Free the port if we took it
                //Debug.LogError("NetworkDiscoveryBase StartDiscovery Exception");
                Shutdown();
                throw;
            }
        }

        public void Shutdown()
        {
            if (serverThread_ != null)
            {
                try
                {
                    serverThread_.Abort();
                }
                catch (Exception)
                {
                    // it is just close, swallow the error
                }

                serverThread_ = null;
            }

            if (clientThread_ != null)
            {
                try
                {
                    clientThread_.Abort();
                }
                catch (Exception)
                {
                    // it is just close, swallow the error
                }

                clientThread_ = null;
            }
        }
    }
}