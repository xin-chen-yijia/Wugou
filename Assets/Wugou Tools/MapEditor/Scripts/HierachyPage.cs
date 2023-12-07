using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wugou.UI;

namespace Wugou.MapEditor.UI
{
    public class HierachyPage : UIBaseWindow
    {
        public GameObject itemContainer;
        public GameObject itemPrefab;

        public TMP_InputField searchInput;

        private Dictionary<GameObject, GameObject> itemsMap_ = new Dictionary<GameObject, GameObject>();
        GameObject lastCheckObj = null;


        private void OnSelectGameEntity(GameObject obj)
        {

            if (itemsMap_.ContainsKey(obj))
            {
                if (lastCheckObj != null)
                {
                    lastCheckObj?.SetActive(false);
                }
                lastCheckObj = itemsMap_[obj].transform.Find("Checked").gameObject;
                lastCheckObj.SetActive(true);
            }

        }

        private void OnAddGameEntity(GameObject obj)
        {
            var go = GameObject.Instantiate<GameObject>(itemPrefab, itemContainer.transform);
            go.SetActive(true);
            AddObject(go, obj.GetComponent<GameEntity>());
        }

        private void OnRemoveGameEntity(GameObject obj)
        {
            RemoveObject(obj);
        }

        private void OnLoadedGameMap()
        {
            // ÓÃGameWorldµÄEntityÌî³ä
            Utils.FillContent(itemContainer, itemPrefab, GameWorld.gameEntities, (go, entity) =>
            {
                AddObject(go, entity);
            });
        }


        // Start is called before the first frame update
        void Start()
        {
            MapEditorSystem.onSelectGameEntity.AddListener(OnSelectGameEntity);
            MapEditorSystem.onGameEntityAdd.AddListener(OnAddGameEntity);
            MapEditorSystem.onGameEntityRemoved.AddListener(OnRemoveGameEntity);
            MapEditorSystem.onLoadedMap.AddListener(OnLoadedGameMap);

            // ËÑË÷
            searchInput.onValueChanged.AddListener((val) =>
            {
                foreach(var v in itemsMap_)
                {
                    v.Value.SetActive(v.Key.GetComponent<GameEntity>().entityName.Contains(val,System.StringComparison.OrdinalIgnoreCase));
                }
            });
        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        private void AddObject(GameObject go, GameEntity entity)
        {
            go.transform.Find("Name").GetComponent<TMP_Text>().text = entity.name;

            go.GetComponent<Button>().onClick.AddListener(() =>
            {
                MapEditorSystem.instance.SelectObject(entity.gameObject);

                if(lastCheckObj != null)
                {
                    lastCheckObj?.SetActive(false);
                }
                lastCheckObj = go.transform.Find("Checked").gameObject;
                lastCheckObj.SetActive(true);
            });


            //
            itemsMap_.Add(entity.gameObject,go);
        }

        private void RemoveObject(GameObject obj)
        {
            if(lastCheckObj && lastCheckObj.transform.parent.gameObject == obj)
            {
                lastCheckObj = null;
            }

            if (itemsMap_.ContainsKey(obj))
            {
                GameObject.Destroy(itemsMap_[obj]);
                itemsMap_.Remove(obj);
            }
        }

        private void OnDestroy()
        {
            MapEditorSystem.onSelectGameEntity.RemoveListener(OnSelectGameEntity);
            MapEditorSystem.onGameEntityAdd.RemoveListener(OnAddGameEntity);
            MapEditorSystem.onGameEntityRemoved.RemoveListener(OnRemoveGameEntity);
            MapEditorSystem.onLoadedMap.RemoveListener(OnLoadedGameMap);
        }
    }

}
