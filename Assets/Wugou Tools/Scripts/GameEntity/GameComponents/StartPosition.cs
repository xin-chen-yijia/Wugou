using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Wugou.PathWalker;

namespace Wugou
{
    public class StartPosition : GameComponent
    {
        public int id { get;private set; }

        private static Dictionary<int,StartPosition> allPositions = new Dictionary<int,StartPosition>();
        public static int positionCount => allPositions.Count;

        public static Transform positionAt(int id) => allPositions[id].transform;

        public override void BeginPlay()
        {
            id = allPositions.Count;
            allPositions.Add(id, this);

            // hide on game
            gameObject.SetActive(false);
        }

        public override void EndPlay()
        {
            // 注意清除，否则会记录到下一个场景。。。
            allPositions.Remove(id);
        }
    }
}

