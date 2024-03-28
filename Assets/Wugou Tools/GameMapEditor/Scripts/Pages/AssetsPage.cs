using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou.UI;
using Wugou.Editor;
using Wugou;
using TMPro;

namespace Wugou.Editor.UI
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
                    List<EditorAssetItem> items = new List<EditorAssetItem>();
                    for (int i = 0; i < GameMapEditor.instance.groupCount; ++i)
                    {
                        for (int j = 0; j < GameMapEditor.instance.GetItemsCount(i); ++j)
                        {
                            var assetItem = GameMapEditor.instance.GetItem(i, j);
                            if (assetItem.name.Contains(value))
                            {
                                items.Add(assetItem);
                            }
                        }
                    }

                    MainThreadTask task = new MainThreadTask();
                    Utils.FillContent(searchResultContainer, draggabeItemPrefab, items, (GameObject go, EditorAssetItem assetItem) =>
                    {
                        IntantiateAssetItem(go, assetItem, task);
                    }, true);

                    iconUpdateTasks_.Add(task);
                    task.Start(20);

                }


            });

            GameMapEditor.onLoadedMap.AddListener(() =>
            {
                UpdateBoard();
                HideLoadingMask();
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
            List<int> groups = GenerateList(GameMapEditor.instance.groupCount);
            Utils.FillContent(sectionContainer, sectionPrefab, groups, (GameObject section, int group) =>
            {
                List<int> items = GenerateList(GameMapEditor.instance.GetItemsCount(group));
                GameObject content = section.GetComponent<UICollapsibleView>().content.gameObject;
                Utils.FillContent(content, draggabeItemPrefab, items, (GameObject go, int itemIndex) =>
                {
                    var assetItem = GameMapEditor.instance.GetItem(group, itemIndex);
                    IntantiateAssetItem(go, assetItem, task);
                }, false);

                var view = section.GetComponent<UICollapsibleView>();
                view.head = GameMapEditor.instance.GetGroupName(group);
                view.Resize();
            });

            iconUpdateTasks_.Add(task);
            task.Start(10);
        }

        private void IntantiateAssetItem(GameObject go, EditorAssetItem assetItem, MainThreadTask task)
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
                var tex = await GameAssetDatabase.GetAssetAsync<Sprite>(assetItem.icon);
                if (tex != null && task.isRunning)
                {
                    // go 可能在加载过程中被删除了
                    if (go)
                    {
                        go.transform.Find("Icon").GetComponent<Image>().sprite = tex;
                    }
                }

            });

            go.SetActive(true);

            go.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                GameMapEditor.instance.PickUp(assetItem);
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
