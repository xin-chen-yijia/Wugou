using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Wugou
{
    /// <summary>
    /// 轻量化的一个文件系统，用于统一AB包、本地资源管理
    /// 借鉴Linux和UE文件挂载的概念
    /// </summary>
    public abstract class LiteFileSystemBase
    {
        public abstract Task<T> GetAsset<T>(string path) where T : UnityEngine.Object;

        public virtual Task<bool> Load() { return Task.FromResult(true); }

        public virtual void Unload() { }
    }
}

