using Mirror;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Wugou.Multiplayer
{
    /// <summary>
    /// �༭��Ϸ��ͼʱ��ӵĶ����ǵ����ģ�Ϊ����Ӧ������Ϸ���������ⴴ��һ������������Ĵ�������
    /// </summary>
    public class GameEntityNetworkify : MonoBehaviour
    {
        public GameObject networkEntityPrefab;

        // Start is called before the first frame update
        void Start()
        {
            if(GamePlay.isGaming)
            {
                // ��ӵ�������ʵ�������б���
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
        /// ������start����Ҫ��gameworld�����곡������gameentity������NetworkBindGameEntity���Ҳ���
        /// </summary>
        public void NetworkInstantiate()
        {
            if (!GamePlay.isGaming)
            {
                Logger.Warning("NetworkInstantiate on editor mode...");
                return;
            }
            // ע�⣺�༭����ʱ������
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

            // ɾ���ѱ���������
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
