using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou.UI;
using Wugou.MapEditor;
using Wugou;
using TMPro;

namespace Wugou.MapEditor.UI
{
    public class AssetsPage : UIBaseWindow
    {
        public GameObject sectionContainer;
        public GameObject sectionPrefab;
        public GameObject draggabeItemPrefab;

        public Button folderButton;
        public Button unFolderButton;

        public GameObject mainPage;
        public GameObject searchResultPage;
        public TMP_InputField searchInput;
        public GameObject searchResultContainer;
        public GameObject loadingMask;

        private List<MainThreadTask> iconUpdateTasks_ = new List<MainThreadTask>();

        // Start is called before the first frame update
        void Start()
        {
            folderButton.onClick.AddListener(() =>
            {
                mainPage.SetActive(false);
                unFolderButton.gameObject.SetActive(true);
                folderButton.gameObject.SetActive(false);
            });

            unFolderButton.onClick.AddListener(() =>
            {
                mainPage.gameObject.SetActive(true);
                unFolderButton.gameObject.SetActive(false);
                folderButton.gameObject.SetActive(true);
            });

            // 搜索
            searchInput.onSubmit.AddListener((string value) =>
            {
                bool nullSearch = string.IsNullOrEmpty(value);
                mainPage.SetActive(nullSearch);
                searchResultPage.SetActive(!nullSearch);

                if (!nullSearch)
                {
                    List<AssetItem> items = new List<AssetItem>();
                    for (int i = 0; i < MapEditorSystem.instance.groupCount; ++i)
                    {
                        for (int j = 0; j < MapEditorSystem.instance.GetItemsCount(i); ++j)
                        {
                            var assetItem = MapEditorSystem.instance.GetItem(i, j);
                            if (assetItem.name.Contains(value))
                            {
                                items.Add(assetItem);
                            }
                        }
                    }

                    MainThreadTask task = new MainThreadTask();
                    Utils.FillContent(searchResultContainer, draggabeItemPrefab, items, (GameObject go, AssetItem assetItem) =>
                    {
                        IntantiateAssetItem(go, assetItem, task);
                    }, true);

                    iconUpdateTasks_.Add(task);
                    task.Start(20);

                }


            });
        }

        public void UpdateBoard()
        {
            System.Func<int, List<int>> GenerateList = (int count) =>
            {
                var list = new List<int>();
                for (int i = 0; i < count; i++)
                {
                    list.Add(i);
                }

                return list;
            };

            MainThreadTask task = new Wugou.MainThreadTask();
            List<int> groups = GenerateList(MapEditorSystem.instance.groupCount);
            Utils.FillContent(sectionContainer, sectionPrefab, groups, (GameObject section, int group) =>
            {
                List<int> items = GenerateList(MapEditorSystem.instance.GetItemsCount(group));
                GameObject content = section.GetComponent<UICollapsibleView>().content.gameObject;
                Utils.FillContent(content, draggabeItemPrefab, items, (GameObject go, int itemIndex) =>
                {
                    var assetItem = MapEditorSystem.instance.GetItem(group, itemIndex);
                    IntantiateAssetItem(go, assetItem, task);
                    //var tmpAsset = MapEditorSystem.instance.GetItem(group, itemIndex);
                    //go.name = tmpAsset.name;
                    //go.transform.Find("Name").GetComponent<TMP_Text>().text = tmpAsset.name;
                    //go.transform.Find("Icon").GetComponent<Image>().sprite = GameAssetDatabase.GetIcon(tmpAsset.asset);
                    //go.SetActive(true);

                    //go.transform.Find("Interact").GetComponent<Button>().onClick.AddListener(() =>
                    //{
                    //    MapEditorSystem.instance.PickUp(MapEditorSystem.instance.GetItem(group, itemIndex));
                    //});

                }, false);

                var view = section.GetComponent<UICollapsibleView>();
                view.head = MapEditorSystem.instance.GetGroupName(group);
                view.Resize();


                // 最后一个充满窗口
                //if (group == groups.Count - 1)
                //{
                //    float h = 0;
                //    var containerTrans = sectionContainer.transform;
                //    for (int i = 0; i < containerTrans.childCount; i++)
                //    {
                //        h += containerTrans.GetChild(i).GetComponent<RectTransform>().sizeDelta.y;
                //    }

                //    if (h < containerTrans.GetComponent<RectTransform>().sizeDelta.y)
                //    {
                //        var remain = containerTrans.GetComponent<RectTransform>().sizeDelta.y - h;
                //        var sectionTrans = section.GetComponent<RectTransform>();
                //        var sz = sectionTrans.sizeDelta;
                //        sz.y += remain;
                //        sectionTrans.sizeDelta = sz;
                //    }

                //}

            });

            iconUpdateTasks_.Add(task);
            task.Start(10);
        }

        private void IntantiateAssetItem(GameObject go, AssetItem assetItem, MainThreadTask task)
        {
            go.name = assetItem.name;
            go.transform.Find("Name").GetComponent<TMP_Text>().text = assetItem.name;

            // 太耗时,卡死Unity主线程了，分块加载
            //Utils.LoadSpriteFromFileWithWebRequest(GameAssetDatabase.GetIconFullPath(assetItem.asset), new Vector2(0.5f, 0.5f), (sprite) =>
            //{
            //    go.transform.Find("Icon").GetComponent<Image>().sprite = sprite;
            //});

            task.AddTask(async () =>
            {
                var tex = await GameAssetDatabase.GetAssetIconAsync(assetItem.asset);
                if (tex != null && task.isRunning)
                {
                    go.transform.Find("Icon").GetComponent<Image>().sprite = tex;
                }

            });

            go.SetActive(true);

            go.transform.Find("Interact").GetComponent<Button>().onClick.AddListener(() =>
            {
                MapEditorSystem.instance.PickUp(assetItem);
            });
        }

        public void HideLoadingMask()
        {
            loadingMask.SetActive(false);
        }

        public void OnApplicationQuit()
        {
            foreach (var v in iconUpdateTasks_)
            {
                v.Stop();
            }
            iconUpdateTasks_.Clear();
        }


    }

}
