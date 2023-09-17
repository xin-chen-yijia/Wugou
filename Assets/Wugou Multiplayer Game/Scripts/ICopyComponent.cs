using Wugou;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 复制组件内容
/// </summary>
public interface ICopyComponent 
{
    public void CopyFrom(GameComponent component);
}
