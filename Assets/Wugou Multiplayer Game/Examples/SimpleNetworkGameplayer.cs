using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Multiplayer;

namespace Wugou.Examples
{
    public class SimpleNetworkGameplayer : MultiplayerGamePlayer
    {
        public override void OnStopClient()
        {
            print("NetworkBehaviour OnStopClient ===== ");
        }

        //public override string GetVisualAssetName()
        //{
        //    return "";
        //}
    }

}
