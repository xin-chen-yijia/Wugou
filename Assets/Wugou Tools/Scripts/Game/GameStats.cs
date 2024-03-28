using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using System;

namespace Wugou
{

    /// <summary>
    /// 游戏信息基类
    /// </summary>
    public class GameStats {
        public string name;
        public string gamemap;
        //public string startTime;
        public float duration;
    }
}
