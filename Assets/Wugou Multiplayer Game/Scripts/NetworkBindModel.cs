using Wugou.MapEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mirror;
using System.Reflection;
using System.IO.IsolatedStorage;

namespace Wugou.Multiplayer
{
    public class NetworkBindModel : NetworkBehaviour
    {

        [SyncVar(hook = nameof(BindMode))]
        public int sceneObjectIndex = -1;

        void BindMode(int oldIndex, int newIndex)
        {
            if (MultiplayerGameManager.singleton is MultiplayerGameManager manager)
            {
                if (sceneObjectIndex > -1)
                {
                    var go = GameWorld.GetGameObject(sceneObjectIndex);
                    transform.position = go.transform.position;
                    transform.rotation = go.transform.rotation;
                    foreach (Transform t in go.transform)
                    {
                        t.SetParent(transform);
                    }

                    name = go.name;

                    // copy component's value
                    foreach (var vv in GetComponents<MonoBehaviour>())
                    {
                        var tp = vv.GetType();
                        var copyFromAttribute = tp.GetCustomAttribute<CanCopyFromAttribute>();
                        if (copyFromAttribute != null)
                        {
                            var comp = go.GetComponent(copyFromAttribute.component);
                            if (comp && (vv is ICopyComponent icpy))
                            {
                                //icpy.CopyFrom(comp);
                            }
                        }
                    }

                    // destroy old protype object
                    GameObject.Destroy(go);
                }
            }
        }
    }
}
