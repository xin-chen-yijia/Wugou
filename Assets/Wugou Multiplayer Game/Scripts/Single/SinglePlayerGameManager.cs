using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Wugou.UI;
using UnityEngine.SceneManagement;

namespace Wugou.Multiplayer 
{
    /// <summary>
    /// 单人模式，同样基于多人模式代码，这样可以公用一套代码
    /// </summary>
    public class SinglePlayerGameManager : NetworkManager
    {
        public static SinglePlayerGameManager instance { get; private set; }

        public GameMission currentMissionPrefab { get; protected set; }

        protected string gameMapPackagePath_; 

        public override void Awake()
        {
            base.Awake();
            instance = this;
        }

        public override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }

            if((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyUp(KeyCode.Q))
            {
                StopGameplay();
            }
        }

        // Update is called once per frame
        //void Update()
        //{

        //}
        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            Wugou.Logger.DebugInfo("OnServerReady");
            // This fires from a Ready message client sends to server after loading the online scene
            base.OnServerReady(conn);

            if (conn.identity == null)
            {
                StartCoroutine(AddPlayerDelayed(conn));   // 
            }
        }

        protected bool isStartedGameplay = false;

        IEnumerator AddPlayerDelayed(NetworkConnectionToClient conn)
        {
            // Wait for server to async load all subscenes for game instances
            while (!isStartedGameplay)
                yield return null;

            //// Send Scene msg to client telling it to load the first additive scene
            //conn.Send(new SceneMessage { sceneName = additiveScenes[0], sceneOperation = SceneOperation.LoadAdditive, customHandling = true });

            // We have Network Start Positions in first additive scene...pick one
            Transform start = StartPosition.positionAt(0);

            // Instantiate player as child of start position - this will place it in the additive scene
            // This also lets player object "inherit" pos and rot from start position transform
            GameObject player = Instantiate(playerPrefab, start);
            // now set parent null to get it out from under the Start Position object
            player.transform.SetParent(null);

            // Wait for end of frame before adding the player to ensure Scene Message goes first
            yield return new WaitForEndOfFrame();

            // Finally spawn the player object for this connection
            NetworkServer.AddPlayerForConnection(conn, player);
        }


        public override async void OnStartClient()
        {
            base.OnStartClient();

            Gameplay.isGaming = true;

            // loading page, hide when all player ready
            DaemonUI.loadingPage.Show();
            DaemonUI.loadingPage.SetProgress(0);

            await new YieldInstructionAwaiter(new WaitForSeconds(0.05f));
            DaemonUI.loadingPage.SetProgress(0.1f); // 避免长时间不动，让人以为出bug了

            DaemonUI.loadingPage.UpdateProgressBar(() =>
            {
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

            // set server flag to stop processing messages while changing scenes
            // it will be re-enabled in FinishLoadScene.
            NetworkServer.isLoadingScene = true;

            // 加载地图
            var succ = await GameWorld.LoadGameMapPackage(gameMapPackagePath_, LoadSceneMode.Single);
            if (succ == GameWorld.Error.kSuccess)
            {
                // 模拟Mirror中对Scene GameObject的处理，参照NetworkIdentity.SetSceneIdSceneHashPartInternal
                for (int i = 0; i < GameWorld.gameEntities.Count; i++)
                {
                    var entity = GameWorld.gameEntities[i];
                    var identity = entity.GetComponent<NetworkIdentity>();
                    if (identity)
                    {
                        // 使用GameEntity的id作为SceneID
                        var sceneId = (ulong)entity.id;

                        string scenePath = GameWorld.activeScene.name.ToLower();

                        // get deterministic scene hash
                        uint pathHash = (uint)scenePath.GetStableHashCode();

                        // shift hash from 0x000000FFFFFFFF to 0xFFFFFFFF00000000
                        ulong shiftedHash = (ulong)pathHash << 32;

                        // OR into scene id
                        sceneId = (sceneId & 0xFFFFFFFF) | shiftedHash;

                        identity.sceneId = sceneId;
                        identity.gameObject.SetActive(false);
                    }

                }

                // attention: mirror
                FinishLoadScene();

                await new YieldInstructionAwaiter(new WaitForSeconds(0.1f));

                // TODO:

                isStartedGameplay = true;

                // 加载任务
                var missionObj = Instantiate<GameObject>(currentMissionPrefab.gameObject);
                await missionObj.GetComponent<GameMission>().Init();
            }
            else
            {
                DaemonUI.makeSurePage.Tips("加载游戏脚本错误！");
            }

            DaemonUI.loadingPage.Hide();
        }

        public virtual void StartGameplay(GameMission mission, string packagePath)
        {
            gameMapPackagePath_ = packagePath;
            currentMissionPrefab = mission;

            StartHost();
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();

            // 先卸载地图
            GameWorld.UnloadGameMap();
        }

        public virtual void StopGameplay()
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

    }
}

