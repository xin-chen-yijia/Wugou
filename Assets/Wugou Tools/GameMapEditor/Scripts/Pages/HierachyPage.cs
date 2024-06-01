using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wugou.UI;

namespace Wugou.Editor.UI
{
    public class HierachyPage : UIBaseWindow
    {
        public GameObject itemContainer;
        public GameObject itemPrefab;

        public TMP_InputField searchInput;

        private Dictionary<GameEntity, GameObject> itemsMap_ = new Dictionary<GameEntity, GameObject>();
        GameObject lastCheckObj = null;


        private void OnSelectGameEntity(GameEntity obj)
        {
            if(obj == null)
            {
                if (lastCheckObj != null)
                {
                    lastCheckObj?.SetActive(false);
                    lastCheckObj = null;
                }
            }
            else if (itemsMap_.ContainsKey(obj))
            {
                if (lastCheckObj != null)
                {
                    lastCheckObj?.SetActive(false);
                }
                lastCheckObj = itemsMap_[obj].transform.Find("Checked").gameObject;
                lastCheckObj.SetActive(true);
            }

        }

        private void OnAddGameEntity(GameEntity entity)
        {
            int entityId = entity.id;

            GameMapEditor.instance.Undo.Record(new CommonObjectRecord(() =>
            {
                var tmp = GameWorld.GetGameEntity(entityId);
                Debug.Assert(tmp != null);
                AddGameEntityInternal(tmp);
            },
            () =>
            {
                var tmp = GameWorld.GetGameEntity(entityId);
                Debug.Assert(tmp != null);
                // TODO: 应该恢复到之前的位置，同时恢复选中状态
                RemoveEntityInternal(tmp);
            }));

            AddGameEntityInternal(entity);
        }

        private void AddGameEntityInternal(GameEntity entity)
        {
            var go = GameObject.Instantiate<GameObject>(itemPrefab, itemContainer.transform);
            go.SetActive(true);
            AddObject(go, entity);

            Utils.ResizeContainerHeight(itemContainer);
        }

        private void OnRemoveGameEntity(GameEntity entity)
        {
            RemoveEntity(entity);
        }

        private void OnLoadedGameMap()
        {
            // 用GameWorld的Entity填充
            Utils.FillContent(itemContainer, itemPrefab, GameWorld.gameEntities.FindAll((entity) => !entity.isStatic), (go, entity) =>
            {
                AddObject(go, entity);
            });
        }


        // Start is called before the first frame update
        void Start()
        {
            GameMapEditor.onSelectGameEntity.AddListener(OnSelectGameEntity);
            GameMapEditor.onGameEntityAdd.AddListener(OnAddGameEntity);
            GameMapEditor.onGameEntityDestory.AddListener(OnRemoveGameEntity);
            GameMapEditor.onLoadedMap.AddListener(OnLoadedGameMap);

            // 搜索
            searchInput.onValueChanged.AddListener((val) =>
            {
                foreach(var v in itemsMap_)
                {
                    v.Value.SetActive(v.Key.GetComponent<GameEntity>().name.Contains(val,System.StringComparison.OrdinalIgnoreCase));
                }
            });
        }

        // Update is called once per frame
        void Update()
        {
            // 更新名称，考虑过aop，但因为Unity对Mono的魔改和dll的加载流程，使得需要新建工程打包dll再以plugins的方式导入，流程麻烦，简单赋值
            // TODO: 对象太多，考虑只更新列表中可见的那些行
            foreach(var item in itemsMap_)
            {
                item.Value.transform.Find("Name").GetComponent<TMP_Text>().text = item.Key.name;
            }
        }

        private void AddObject(GameObject go, GameEntity entity)
        {
            go.transform.Find("Name").GetComponent<TMP_Text>().text = entity.name;

            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                using(var transaction = new GameMapEditor.TransactionScope())
                {
                    transaction.Record(new EditorSelectGameEntity());

                    GameMapEditor.instance.SelectGameEntity(entity);
                }

                if(lastCheckObj != null)
                {
                    lastCheckObj?.SetActive(false);
                }
                lastCheckObj = go.transform.Find("Checked").gameObject;
                lastCheckObj.SetActive(true);
            });


            //
            itemsMap_.Add(entity,go);
        }

        private void RemoveEntity(GameEntity entity)
        {
            if (!entity)
            {
                return;
            }

            int entityId = entity.id;
            GameMapEditor.TransactionScope.activeTransaction?.Record(new CommonObjectRecord(() =>
            {
                var tmp = GameWorld.GetGameEntity(entityId);
                Debug.Assert(tmp != null);
                RemoveEntityInternal(tmp);
            },
            () =>
            {
                var tmp = GameWorld.GetGameEntity(entityId);
                Debug.Assert(tmp != null);
                // TODO: 应该恢复到之前的位置，同时恢复选中状态
                AddGameEntityInternal(tmp);
            }));

            RemoveEntityInternal(entity);
        }

        private void RemoveEntityInternal(GameEntity entity)
        {
            if (lastCheckObj && lastCheckObj.transform.parent.gameObject == entity)
            {
                lastCheckObj = null;
            }

            if (itemsMap_.ContainsKey(entity))
            {
                GameObject.Destroy(itemsMap_[entity]);
                itemsMap_.Remove(entity);
            }
        }

        private void OnDestroy()
        {
            GameMapEditor.onSelectGameEntity.RemoveListener(OnSelectGameEntity);
            GameMapEditor.onGameEntityAdd.RemoveListener(OnAddGameEntity);
            GameMapEditor.onGameEntityDestory.RemoveListener(OnRemoveGameEntity);
            GameMapEditor.onLoadedMap.RemoveListener(OnLoadedGameMap);
        }
    }

}
