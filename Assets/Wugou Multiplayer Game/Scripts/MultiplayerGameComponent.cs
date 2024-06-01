using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Wugou.Multiplayer
{
    /// <summary>
    /// 用于多人情况下的GameComponent
    /// </summary>
    public class MultiplayerGameComponent : NetworkBehaviour
    {
        public virtual void Awake() { }

        /// <summary>
        /// 不是太推荐使用，只是为了少写点代码
        /// 很多时候，我们需要在所有的客户端执行同样的操作，这样需要写一个Command函数和一个ClientRPC函数，比较麻烦，用这个简单替代一下
        /// 因为使用的json序列化和反射，性能可能会差一些
        /// 同时，对于类型，Type.GetType使用的是当前程序集，为了支持unity的一些类型，额外加入"UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"的程序集
        /// 
        /// 注意：传浮点数的话，可能会有误差累计，所以浮点数最好是状态数值，而非变量数值（类似移动了多少米）
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="methodName"></param>
        /// <param name="val"></param>
        public void CallOnAllClient<T>(string methodName, T val)
        {
            CmdCall(methodName, typeof(T).AssemblyQualifiedName, Newtonsoft.Json.JsonConvert.SerializeObject(val, JsonSerializerGlobal.commonConverts));
        }

        public void CallOnAllClientNonAuthority<T>(string methodName, T val)
        {
            CmdCallNonAuthority(methodName, typeof(T).AssemblyQualifiedName, Newtonsoft.Json.JsonConvert.SerializeObject(val, JsonSerializerGlobal.commonConverts));
        }

        /// <summary>
        /// 底层使用的SendMessage
        /// </summary>
        /// <param name="methodName"></param>
        public void CallOnAllClient(string methodName)
        {
            CmdCall(methodName);
        }

        #region Mirror RPC

        [Command]
        private void CmdCall(string methodName)
        {
            RpcCall(methodName);
        }

        [ClientRpc]
        private void RpcCall(string methodName)
        {
            SendMessage(methodName);
        }

        [Command]
        private void CmdCall(string methodName, string valType, string val)
        {
            RpcCall(methodName, valType, val);
        }

        [Command(requiresAuthority = false)]
        private void CmdCallNonAuthority(string methodName, string valType, string val)
        {
            RpcCall(methodName, valType, val);
        }

        [ClientRpc]
        private void RpcCall(string methodName, string valType, string val)
        {
            var type = System.Type.GetType(valType);
            if (type == null)
            {
                type = typeof(Vector3).Assembly.GetType(valType);
            }
            Debug.Assert(type != null);
            var obj = Newtonsoft.Json.JsonConvert.DeserializeObject(val, type, JsonSerializerGlobal.commonConverts);
            SendMessage(methodName, obj);
        }

        [Command(requiresAuthority = false)]
        public void CmdAssignAuthority(int playerId)
        {
            var identity = GetComponent<NetworkIdentity>();
            if (playerId != -1)
            {
                var player = MultiplayerGamePlayer.Get(playerId) as MultiplayerGamePlayer;
                identity.AssignClientAuthority(player.connectionToClient);
            }
            else
            {
                Wugou.Logger.Warning($"Assign authority to invalid id. id: {playerId}");
            }
        }

        [Command]
        public void CmdRemoveAuthority()
        {
            var identity = GetComponent<NetworkIdentity>();
            if (identity.connectionToClient != null)
            {
                identity.RemoveClientAuthority();
            }
        }

        #endregion
    }
}
