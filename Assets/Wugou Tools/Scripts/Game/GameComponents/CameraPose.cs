using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Editor;

namespace Wugou
{
    /// <summary>
    /// 摄像机姿态，主要用于记录摄像机的方位，方便控制摄像机
    /// </summary>
    [CustomGameComponentView(typeof(CameraPoseView))]
    public class CameraPose : GameComponent
    {
    }
}
