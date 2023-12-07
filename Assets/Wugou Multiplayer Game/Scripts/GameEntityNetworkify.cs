using Mirror;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Wugou.Multiplayer
{
    /// <summary>
    /// 编辑游戏地图时添加的对象是单机的，为了适应多人游戏场景，额外创建一个带网络组件的代理物体
    /// </summary>
    public class GameEntityNetworkify : MonoBehaviour
    {
        public GameObject networkEntityPrefab;

        // Start is called before the first frame update
        void Start()
        {
            if(GamePlay.isGaming)
            {
                // 添加到可网络实例化的列表中
                if (!MultiplayerGameManager.instance.spawnPrefabs.Contains(networkEntityPrefab))
                {
                    MultiplayerGameManager.instance.spawnPrefabs.Add(networkEntityPrefab);
                    NetworkClient.RegisterPrefab(networkEntityPrefab);
                }
            }

        }

        //// Update is called once per frame
        //void Update()
        //{

        //}

        /// <summary>
        /// 不放在start中是要等gameworld加载完场景后处理gameentity，否则NetworkBindGameEntity会找不到
        /// </summary>
        public void NetworkInstantiate()
        {
            if (!GamePlay.isGaming)
            {
                Logger.Warning("NetworkInstantiate on editor mode...");
                return;
            }
            // 注意：编辑场景时不触发
            // create network
            if (MultiplayerGameManager.instance.mode == Mirror.NetworkManagerMode.Host || MultiplayerGameManager.instance.mode == Mirror.NetworkManagerMode.ServerOnly)
            {
                var go = Instantiate<GameObject>(networkEntityPrefab);
                go.transform.position = transform.position;
                go.transform.rotation = transform.rotation;

                var setting = new JsonSerializerSettings() { Converters = JsonSerializerGlobal.commonConverts };
                // networkify components copy data
                foreach (var comp in go.GetComponents<NetworkBehaviour>())
                {
                    var tt = comp.GetType().GetCustomAttribute<NetworkDataFromAttribute>();
                    if (tt != null)
                    {
                        var scomp = gameObject.GetComponent(tt.targetType);
                        if (scomp)
                        {
                            JsonConvert.PopulateObject(JsonConvert.SerializeObject(scomp, setting), comp, setting);
                        }
                    }
                }

                var bindComp = go.GetComponent<NetworkCopyGameEntity>();
                if (bindComp != null)
                {
                    bindComp.entityID = GetComponent<GameEntity>().id;
                }

                NetworkServer.Spawn(go);
            }

            // 删除已被代理的组件
            foreach (var comp in networkEntityPrefab.GetComponents<NetworkBehaviour>())
            {
                var tt = comp.GetType().GetCustomAttribute<NetworkDataFromAttribute>();
                if (tt != null)
                {
                    var scomp = gameObject.GetComponent(tt.targetType);
                    if (scomp)
                    {
                        GameObject.Destroy(scomp);
                    }
                }
            }

            GameObject.Destroy(this);
        }
    }

}
