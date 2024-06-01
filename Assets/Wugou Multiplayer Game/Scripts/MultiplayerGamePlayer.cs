using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Threading.Tasks;
using UnityEngine.EventSystems;

namespace Wugou.Multiplayer
{

    public class MultiplayerGamePlayer : MultiplayerGameComponent
    {
        /// <summary>
        /// 单例
        /// </summary>
        public static MultiplayerGamePlayer owner;

        [SyncVar(hook = nameof(PlayerNameChanged))]
        public string playerName="";

        [SyncVar]
        public int playerId;

        [SyncVar(hook = nameof(PlayerRoleChanged))]
        public int playerRole;

        //
        public float maxSelectDistance = 100;

        /// <summary>
        /// 记录当前玩家
        /// </summary>
        public static Dictionary<int, MultiplayerGamePlayer> allPlayers { get; private set; } = new Dictionary<int, MultiplayerGamePlayer>();

        /// <summary>
        /// 获取玩家实例
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static MultiplayerGamePlayer Get(int id)
        {
            if (allPlayers.ContainsKey(id))
            {
                return allPlayers[id];
            }

            Wugou.Logger.Error($"Player id {id} not exist. Maybe not login or connect timeout(auto disconnect).");
            return null;
        }

        protected virtual void PlayerNameChanged(string oldName, string newName)
        {
            name = newName;
        }

        protected virtual void PlayerRoleChanged(int oldRole, int newRole) 
        { 
            
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // 记录
            allPlayers[this.playerId] = this;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            MultiplayerGameManager.instance?.UpdateGameplayerSnapshot(this);
        }

        public override void OnStartLocalPlayer()
        {
            print("OnStartLocalPlayer");
            base.OnStartLocalPlayer();

            owner = this;
        }

        public override void OnStopLocalPlayer()
        {
            base.OnStopLocalPlayer();

            owner = null;
        }

        /// <summary>
        /// 在场景中实例化GameEntity，主要用于多人网络环境下
        /// </summary>
        /// <param name="info"></param>
        public async void InstantiateGameEntity(InstantiateGameEntityParams info)
        {
            var entity = GameEntityManager.CreateGameEntity(info.assetName, info.prototype);
            await entity.InstantiateBody();
            entity.id = info.entityID;
            entity.position = info.pos;
            entity.rotation = info.quaternion;
            entity.name = info.instantiateName;
            GameWorld.AddExistGameEntity(entity);
        }

        /// <summary>
        /// 删除GameEntity,主要用于多人网络环境下
        /// </summary>
        /// <param name="entityID"></param>
        public void DestroyGameEntity(int entityID)
        {
            var entity = GameWorld.GetGameEntity(entityID);
            if (entity)
            {
                GameWorld.DestroyGameEntity(entity);
            }
            else
            {
                Wugou.Logger.Error($"Not found GameEntity： {entityID}");
            }
        }

        /// <summary>
        /// 客户端预测模式下，设置有刚体的
        /// </summary>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        public void SetPositionAndRotationInPredictedRigidbody(Vector3 position,  Quaternion rotation)
        {
            var rigidbody = GetComponent<PredictedRigidbody>().predictedRigidbody;
            rigidbody.position = position;
            rigidbody.rotation = rotation;

            CmdSetPositionAndRotationInPredictedRigidbody(position,rotation );
        }

        /// <summary>
        /// 获取可预测刚体的位置和角度
        /// </summary>
        /// <param name="postion"></param>
        /// <param name="rotation"></param>
        public void GetPositionAndRotationInPredictedRigidbody(out Vector3 postion, out Quaternion rotation)
        {
            var rigidbody = GetComponent<PredictedRigidbody>().predictedRigidbody;
            postion = rigidbody.position;
            rotation = rigidbody.rotation;
        }

        /// <summary>
        /// 从场景中消失，注意不是移除
        /// 角色还在场景中，但其他人看不见他，同时也不能碰触到他
        /// </summary>
        public void DisappearanceFromScene()
        {
            // 
            GetComponent<PredictedRigidbody>().predictedRigidbody.interpolation = RigidbodyInterpolation.None;
            GetComponent<PredictedRigidbody>().predictedRigidbody.isKinematic = true;
            GetComponent<PredictedRigidbody>().predictedRigidbody.position = new Vector3(100000, 0, 0);
        }

        #region Mirror RPC

        /// <summary>
        /// 发送给指定的玩家
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="message"></param>
        [Command]
        public void CmdSendMessageToPlayer(int playerId, string message)
        {
            var netPlayer = Get(playerId);
            netPlayer.TargetSendMessageToPlayer(netPlayer.GetComponent<NetworkIdentity>().connectionToClient, playerName, message);
        }

        [TargetRpc]
        protected void TargetSendMessageToPlayer(NetworkConnectionToClient target, string fromPlayer, string message)
        {
            OnReceivePlayerMessage(fromPlayer, message);
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        protected virtual void OnReceivePlayerMessage(string player, string message)
        {

        }

        /// <summary>
        /// 发送给所有人
        /// </summary>
        /// <param name="message"></param>
        [Command]
        public void CmdSendMessageToAll(string message)
        {
            RpcSendMessageToAll(playerName, message);
        }

        [ClientRpc]
        protected void RpcSendMessageToAll(string player, string message)
        {
            OnReceivePlayerMessage(player, message);
        }


        /// <summary>
        /// 隐藏mesh和collider
        /// </summary>
        /// <param name="allHide">true：所有客户端都隐藏，false：只在其它客户端隐藏</param>
        [Command]
        public void CmdHideMeshAndColliders(bool allHide)
        {
            RpcHideMeshAndColliders(allHide);
        }

        [ClientRpc]
        private void RpcHideMeshAndColliders(bool allHide)
        {
            if (allHide || !isLocalPlayer)
            {
                foreach (var v in GetComponentsInChildren<Renderer>())
                {
                    v.enabled = false;
                }

                foreach(var v in GetComponentsInChildren<Collider>())
                {
                    v.enabled = false;
                }

                // 没有了碰撞，如果有刚体，则会往下掉
                var rb = GetComponent<Rigidbody>();
                if (rb)
                {
                    rb.isKinematic = true;
                }
            }

        }

        /// <summary>
        /// 实例化有网络组件的物体，第一步
        /// </summary>
        /// <param name="info"></param>
        [Command]
        public void CmdInstantiateNetworkGameEntity(InstantiateGameEntityParams info)
        {
            var prefabs = MultiplayerGameManager.instance.spawnPrefabs;
            for (int i = 0; i < prefabs.Count; ++i)
            {
                if (prefabs[i].name == info.prototype)
                {
                    GameObject go = Instantiate<GameObject>(prefabs[i]);
                    go.name = info.instantiateName;
                    go.transform.position = info.pos;
                    go.transform.rotation = info.quaternion;

                    NetworkServer.Spawn(go);

                    RpcInstantiateNetworkGameEntityBody(go.GetComponent<NetworkIdentity>().netId, info);
                    return;
                }
            }

            Wugou.Logger.Error($"{info.prototype} not spawnable...");
        }

        /// <summary>
        /// 实例化有网络组件物体的第二步，查找到GameEntity，实例化Body
        /// </summary>
        /// <param name="info"></param>
        [ClientRpc]
        public async void RpcInstantiateNetworkGameEntityBody(uint netId, InstantiateGameEntityParams info)
        {
            var netObjs = GameObject.FindObjectsOfType<NetworkIdentity>();
            for (int i = 0; i < netObjs.Length; i++)
            {
                if (netObjs[i].netId == netId)
                {
                    var go = netObjs[i].gameObject;
                    go.name = info.instantiateName;
                    var entity = go.GetComponent<GameEntity>();
                    entity.id = info.entityID;
                    entity.asset = info.assetName;
                    entity.prototype = info.prototype;

                    await entity.InstantiateBody();

                    GameWorld.AddExistGameEntity(entity);

                    return;
                }

            }

            Wugou.Logger.Error($"Not found NetworkIdentity: {netId}..");

        }

        [Command]
        public void CmdRemoveGameEntity(GameObject go)
        {
            // 暂时没有缓存的需求，先用删除
            //NetworkServer.UnSpawn(go);
            NetworkServer.Destroy(go);
        }

        [Command]
        public void CmdSetPositionAndRotationInPredictedRigidbody(Vector3 position, Quaternion rotation)
        {
            var rigidbody = GetComponent<Rigidbody>();
            rigidbody.position = position;
            rigidbody.rotation = rotation;
        }

        [Command]
        public void CmdSetGameEntityActive(int entityId, bool active)
        {
            RpcSetGameEntityActive(entityId, active);
        }

        [ClientRpc]
        private void RpcSetGameEntityActive(int entityId, bool active)
        {
            var entity = GameWorld.GetGameEntity(entityId);
            if (entity)
            {
                entity.SetActive(active);
            }
        }

        #endregion

        protected virtual void OnDestroy()
        {
            //
            if (allPlayers.ContainsKey(playerId))
            {
                allPlayers.Remove(playerId);
            }
        }
    }

    /// <summary>
    /// 用于实例化GameEntity的参数
    /// </summary>
    public struct InstantiateGameEntityParams
    {
        public int entityID;
        public string assetName;
        public string prototype;
        public Vector3 pos;
        public Quaternion quaternion;
        public string instantiateName;
    }
}
