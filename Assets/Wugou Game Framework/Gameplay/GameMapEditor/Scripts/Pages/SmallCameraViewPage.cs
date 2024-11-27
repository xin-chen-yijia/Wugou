using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou.UI;

namespace Wugou.Editor.UI
{
    /// <summary>
    /// 显示摄像机的视角，参考Unity选中摄像机
    /// </summary>
    public class SmallCameraViewPage : UIBaseWindow
    {
        public RawImage cameraViewTexture;

        public void SetTexture(RenderTexture texture)
        {
            cameraViewTexture.texture = texture;
        }
    }
}

