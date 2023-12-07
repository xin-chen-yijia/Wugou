using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.Multiplayer
{
    using Mirror;
    using System.Threading.Tasks;

    public class MultiplayerGamePlayer : NetworkBehaviour
    {
        /// <summary>
        /// ����
        /// </summary>
        public static MultiplayerGamePlayer owner;

        [SyncVar(hook = nameof(SetPlayerName))]
        public string playerName;

        [SyncVar]
        public int playerId;

        [SyncVar(hook = nameof(SetPlayerRole))]
        public int playerRole;

        public GameObject visualParent;        // ����Mirror���ͬ���Ƕ�
        public GameObject visualObject { get; protected set; } // ��assetbundle�м��صĽ�ɫģ��

        /// <summary>
        /// �ü��صĶ���
        /// </summary>
        public Animator animator => visualObject.GetComponent<Animator>();

        protected virtual void SetPlayerName(string oldName, string newName)
        {
            name = newName;
        }

        protected virtual void SetPlayerRole(int oldRole, int newRole) 
        { 
            
        }

        /// <summary>
        /// ��ʼ��Ϸ
        /// </summary>
        public virtual void ReadyGo()
        {
        }

        public override async void OnStartClient()
        {
            base.OnStartClient();

            // ��¼
            MultiplayerGameManager.instance.AddGamePlayer(this);

            // ����ģ��
            visualObject = await InstantiateVisualModel();

            OnInstantiateVisualModel();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            MultiplayerGameManager.instance.UpdateGameplayerSnapshot(this);
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
        /// ��ȡ��ɫģ����assetbundle�е�����
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        public virtual string GetVisualAssetName()
        {
            throw new System.Exception("Not implement GetVisualAssetName..");
        }

        /// <summary>
        /// ʵ������AB���е�ģ��
        /// </summary>
        /// <param name="assetName"></param>
        /// <param name="pos"></param>
        /// <param name="quaternion"></param>
        protected async void InstantiateInternal(string assetName, Vector3 pos, Quaternion quaternion, string instantiateName="")
        {
            var loader = await AssetBundleAssetLoader.GetOrCreate(GamePlay.settings.mainAssetbundle);
            var assetPrefab = await loader.LoadAssetAsync<GameObject>(assetName);
            var visualObject = Instantiate<GameObject>(assetPrefab, Vector3.zero, Quaternion.identity);
            visualObject.transform.position = pos;
            visualObject.transform.rotation = quaternion;
            visualObject.name = instantiateName;
        }

        /// <summary>
        /// ʵ������ɫģ��
        /// </summary>
        protected virtual async Task<GameObject> InstantiateVisualModel()
        {
            string visualizeAssetName = GetVisualAssetName(); 
            var loader = await AssetBundleAssetLoader.GetOrCreate(GamePlay.settings.mainAssetbundle);
            var soilderPrefab  = await loader.LoadAssetAsync<GameObject>(visualizeAssetName);
            var visualObject = Instantiate<GameObject>(soilderPrefab, Vector3.zero, Quaternion.identity, visualParent.transform);
            visualObject.transform.localPosition = Vector3.zero;
            visualObject.transform.localRotation = Quaternion.identity;

            return visualObject;
        }

        /// <summary>
        /// ��ʵ����ģ�ͺ����
        /// </summary>
        protected virtual void OnInstantiateVisualModel()
        {

        }

        #region Mirror RPC
        /// <summary>
        /// ���͸�ָ�������
        /// </summary>
        /// <param name="targetPlayerId"></param>
        /// <param name="message"></param>
        [Command]
        public void CmdSendMessageToPlayer(int targetPlayerId, string message)
        {
            var netPlayer = MultiplayerGameManager.instance.GetGameplayer(targetPlayerId);
            netPlayer.TargetSendMessageToPlayer(netPlayer.GetComponent<NetworkIdentity>().connectionToClient, playerName, message);
        }

        [TargetRpc]
        protected void TargetSendMessageToPlayer(NetworkConnectionToClient target, string fromPlayer, string message)
        {
            OnReceivePlayerMessage(fromPlayer, message);
        }

        /// <summary>
        /// ������Ϣ
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        protected virtual void OnReceivePlayerMessage(string player, string message)
        {

        }

        /// <summary>
        /// ���͸�������
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
        /// ��������
        /// </summary>
        /// <param name="path">'/'��ͷ������ȫ�ֲ��ң�ĳ�����ڱ������²���</param>
        /// <returns></returns>
        private GameObject GetGameObjectInternal(string path)
        {
            GameObject obj = null;

            if (path.StartsWith("/"))
            {
                obj = GameObject.Find(path.Substring(1));
            }
            else
            {
                var t = transform.Find(path);
                obj = t ? t.gameObject : null;
            }

            if (!obj)
            {
                Wugou.Logger.Error($"Can't find GameObject {path}");
            }
            return obj;
        }

        /// <summary>
        /// ������ʾ����������
        /// </summary>
        /// <param name="path">'/'��ͷ������ȫ�ֲ��ң�ĳ�����ڱ������²���</param>
        /// <param name="active"></param>
        [Command]
        public void CmdSetGameObjectActive(string path, bool active)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            RpcSetGameObjectActive(path, active);
        }

        [ClientRpc]
        private void RpcSetGameObjectActive(string path, bool active)
        {
            var obj = GetGameObjectInternal(path);
            obj.SetActive(active);
        }

        [Command]
        public void CmdSetGameObjectTransform(string path, Vector3 position,  Quaternion rotation, Space space)
        {
            RpcSetGameObjectTransform(path, position, rotation, space);
        }

        [ClientRpc]
        public void RpcSetGameObjectTransform(string path, Vector3 position, Quaternion rotation, Space space) 
        {
            var obj = GetGameObjectInternal(path);
            if(space == Space.World)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
            }
            else
            {
                obj.transform.localPosition = position;
                obj.transform.localRotation = rotation;
            }
        }

        /// <summary>
        /// ����������Ч
        /// </summary>
        /// <param name="path"></param>
        /// <param name="play"></param>
        [Command]
        public void CmdPlayeParticle(string path, bool play)
        {
            RpcPlayParticle(path, play);
        }

        [ClientRpc]
        private void RpcPlayParticle(string path, bool play)
        {
            var obj = GetGameObjectInternal(path);
            if (obj && obj.GetComponent<ParticleSystem>())
            {
                if (play)
                {
                    obj.GetComponent<ParticleSystem>().Play();
                }
                else
                {
                    obj.GetComponent<ParticleSystem>().Stop();
                }
            }
            else
            {
                Wugou.Logger.Error($"Can't find GameObject or no particle system on: {path}");
            }
        }

        [Command]
        public void CmdInstantiateWithAssetName(string assetName, Vector3 pos, Quaternion quaternion, string instantiateName)
        {
            RpcInstantiateWithName(assetName, pos, quaternion, instantiateName);
        }

        [ClientRpc]
        private void RpcInstantiateWithName(string assetName, Vector3 pos, Quaternion quaternion, string instantiateName)
        {
            InstantiateInternal(assetName, pos, quaternion, instantiateName);
        }

        /// <summary>
        /// ������ʵ������������
        /// </summary>
        [Command]
        public void CmdHideMeshAndCollidersOnOthers()
        {
            RpcHideMeshAndCollidersOnOthers();
        }

        [ClientRpc]
        private void RpcHideMeshAndCollidersOnOthers()
        {
            if (!isLocalPlayer)
            {
                foreach (var v in GetComponentsInChildren<Renderer>())
                {
                    v.enabled = false;
                }

                foreach (var v in GetComponentsInChildren<Renderer>())
                {
                    v.enabled = false;
                }
            }

        }

        /// <summary>
        /// cmd
        /// </summary>
        /// <param name="objName"></param>
        [Command]
        public void CmdDestroyObject(string objName)
        {
            RpcDestroyObject(objName);
        }

        [ClientRpc]
        private void RpcDestroyObject(string objName)
        {
            var go = GetGameObjectInternal(objName);
            if(go != null)
            {
                GameObject.Destroy(go);
            }
        }

        #endregion

        private void OnDestroy()
        {
            //
            MultiplayerGameManager.instance.RemoveGamePlayer(this);
        }
    }
}
