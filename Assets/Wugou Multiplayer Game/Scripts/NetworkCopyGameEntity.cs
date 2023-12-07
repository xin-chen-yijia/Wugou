using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;
using System.Reflection;
using System.IO.IsolatedStorage;
using UnityEngine.Events;

namespace Wugou.Multiplayer
{
    public class NetworkCopyGameEntity : NetworkBehaviour
    {
        /// <summary>
        /// 重新绑定GameEntity后调用
        /// </summary>
        public UnityEvent onBinded = new UnityEvent();

        [SyncVar(hook = nameof(Bind))]
        public int entityID = -1;

        void Bind(int oldIndex, int newIndex)
        {
            if (MultiplayerGameManager.singleton is MultiplayerGameManager manager)
            {
                if (entityID > -1)
                {
                    var go = GameWorld.GetGameEntity(entityID);
                    transform.position = go.transform.position;
                    transform.rotation = go.transform.rotation;
                    while (go.transform.childCount > 0)
                    {
                        go.transform.GetChild(0).SetParent(transform);
                    }

                    // copy GameCompoents(include GameEntity)
                    foreach(var comp in go.GetComponents<GameComponent>())
                    {
                        if(comp.GetType().GetCustomAttribute<CopyToClientAttribute>() != null)
                        {
                            Utils.CopyComponent(gameObject, comp);
                        }
                        else if(MultiplayerGameManager.instance.mode == NetworkManagerMode.Host || MultiplayerGameManager.instance.mode == NetworkManagerMode.ServerOnly)   // not use isServer, because isServer init lag
                        {
                            Utils.CopyComponent(gameObject, comp);   // other GameComponent only run on server
                        }
                    }

                    // replace
                    var newEnvity = GetComponent<GameEntity>();
                    if(newEnvity != null) 
                    {
                        GameWorld.ReplaceGameEntity(go, newEnvity);
                    }


                    gameObject.name = go.name + "(network)"; // 注意：GameEntity会改name，所以放到后面

                    // copy colliders, for script on root's object
                    foreach (var collider in go.GetComponents<Collider>())
                    {
                        var newCollider = Utils.CopyComponent(gameObject, collider);
                        newCollider.enabled = collider.enabled;
                        collider.enabled = false;
                    }

                    // destroy old protype object
                    GameObject.Destroy(go.gameObject);

                    //
                    onBinded?.Invoke();

                }
            }
        }
    }
}
