using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Wugou.Editor
{
    /// <summary>
    /// 操作记录, 借鉴UE4 FObjectRecord
    /// UE4是基于它的序列化实现的，当前工程的GameEntity也有序列化，但我们不会完全复现
    /// 记录几个典型的操作：
    /// 1. 创建GameEntity，这一步只需要记录GameEntity序列化内容，但不需要包括其Component的属性变化内容，这一步是不是跟UE4一样，也引入一个属性是否更改的状态描述？而它的Undo则是删除GameEntity
    /// 2. 删除GameEntity，这一步和创建GameEntity基本一样，但GameEntity的Component可能有变化，这个时候如何保存Component?
    /// 3. 修改GameEntity属性值，同样涉及的是保存Component的值； 
    /// 4. 选择GameEntity；
    /// 5. 暂时不考虑除操作GameEntity外的其它操作；
    /// 
    /// 所以关键在于如何保存GameEntity上的Component的值，不想搞dirty标志（需要给所有的属性都增加Set接口和dirty标志），简单点，保存所有属性，
    /// </summary>
    public abstract class ObjectRecord
    {
        public virtual void Save() { }

        /// <summary>
        /// 返回Task是因为要兼容异步操作
        /// </summary>
        /// <returns></returns>
        public abstract bool Load();

        public abstract bool Revert();

        public virtual void FinalizeDo() { }
    }

    /// <summary>
    /// 通用的一种记录，记录两个回调
    /// </summary>
    public class CommonObjectRecord : ObjectRecord
    {
        private System.Action loadAction_;
        private System.Action revertAction_;

        public CommonObjectRecord(Action loadAction_, Action revertAction_)
        {
            this.loadAction_ = loadAction_;
            this.revertAction_ = revertAction_;
        }

        public override bool Load()
        {
            loadAction_?.Invoke();
            return true;
        }

        public override bool Revert()
        {
            revertAction_?.Invoke();

            return true;
        }

    }

    /// <summary>
    /// 创建GameEntity操作
    /// </summary>
    public class EditorCreateGameEntity : ObjectRecord
    {
        private int id_;
        private string serializedContent_;
        public EditorCreateGameEntity(int id)
        {
            id_ = id;
        }

        public override void Save()
        {
            var entity = GameWorld.GetGameEntity(id_);
            Debug.Assert(entity != null);
            serializedContent_ = JsonConvert.SerializeObject(entity, JsonSerializerGlobal.commonSerializerSettings);
        }

        public override bool Load()
        {
            Debug.Assert(!string.IsNullOrEmpty(serializedContent_));
            var entity = JsonConvert.DeserializeObject<GameEntity>(serializedContent_, JsonSerializerGlobal.commonSerializerSettings);
            Debug.Assert(entity.id == id_);

            GameWorld.AddGameEntity(entity);

            return true;
        }

        public override bool Revert()
        {
            var entity = GameWorld.GetGameEntity(id_);
            if (entity)
            {
                GameWorld.RemoveGameEntity(entity);
                GameEntityManager.DestroyGameEntity(entity);
            }
            else
            {
                Wugou.Logger.Error("EditorCreateGameEntity revert fail..");
            }

            return true;
        }
    }

    public class EditorSelectGameEntity : ObjectRecord
    {
        int oldId_ = -1;
        int newId_ = -1;

        public EditorSelectGameEntity()
        {
            var entity = GameMapEditor.instance.selectedEntity;
            if (entity)
            {
                oldId_ = entity.id;
            }
        }

        public override bool Load()
        {
            var entity = GameWorld.GetGameEntity(newId_);
            GameMapEditor.instance.SelectGameEntity(entity);

            return true;
        }

        public override bool Revert()
        {
            var entity = GameWorld.GetGameEntity(oldId_);
            GameMapEditor.instance.SelectGameEntity(entity);

            return true;
        }

        public override void FinalizeDo()
        {
            var entity = GameMapEditor.instance.selectedEntity;
            if (entity)
            {
                newId_ = entity.id;
            }
        }
    }

    /// <summary>
    /// 移动GameEntity
    /// </summary>
    public class EditorMoveGameEntity : ObjectRecord
    {
        int id_ = -1;

        Vector3 beginPos;
        Vector3 endPos;

        Quaternion beginRotation;
        Quaternion endRotation;

        Vector3 beginScale;
        Vector3 endScale;
        public EditorMoveGameEntity(int id)
        {
            this.id_ = id;

            var entity =GameWorld.GetGameEntity(id_);
            if (entity)
            {
                beginPos = entity.transform.position;
                beginRotation = entity.transform.rotation;
                beginScale = entity.transform.localScale;
            }
            else
            {
                Wugou.Logger.Error("EditorMoveGameEntity with a not exist entity...");
            }
        }

        public override void FinalizeDo()
        {
            var entity = GameWorld.GetGameEntity(id_);
            if (entity)
            {
                endPos = entity.transform.position;
                endRotation = entity.transform.rotation;
                endScale = entity.transform.localScale;
            }
        }

        public override bool Load()
        {
            var entity = GameWorld.GetGameEntity(id_);
            if (entity)
            {
                entity.transform.position = endPos;
                entity.transform.rotation = endRotation;
                entity.transform.localScale = endScale;
            }

            return true;
        }

        public override bool Revert()
        {
            var entity = GameWorld.GetGameEntity(id_);
            if (entity)
            {
                entity.transform.position = beginPos;
                entity.transform.rotation = beginRotation;
                entity.transform.localScale = beginScale;
            }

            return true;
        }
    }

    public class EditorDeleteGameEntity : ObjectRecord
    {
        int id_ = -1;
        string serializedContent_;

        public EditorDeleteGameEntity(int id)
        {
            this.id_ = id;

            var entity = GameWorld.GetGameEntity(id);
            serializedContent_ = JsonConvert.SerializeObject(entity, JsonSerializerGlobal.commonSerializerSettings);
        }

        public override bool Load()
        {
            var entity = GameWorld.GetGameEntity(id_);
            GameWorld.RemoveGameEntity(entity);
            GameEntityManager.DestroyGameEntity(entity);

            return true;
        }

        public override bool Revert()
        {
            Debug.Assert(!string.IsNullOrEmpty(serializedContent_));
            var entity = JsonConvert.DeserializeObject<GameEntity>(serializedContent_, JsonSerializerGlobal.commonSerializerSettings);
            Debug.Assert(entity.id == id_);
            GameWorld.AddGameEntity(entity);

            return true;
        }
    }
}

