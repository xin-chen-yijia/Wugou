using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou.UI;
using Wugou.MapEditor;
using Wugou;
using TMPro;
using System.IO;

public class AssetsPage : UIBaseWindow
{
    public GameObject sectionContainer;
    public GameObject sectionPrefab;
    public GameObject draggabeItemPrefab;

    // Start is called before the first frame update
    void Start()
    {
        transform.Find("FolderButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            transform.Find("Main").gameObject.SetActive(false);
            transform.Find("UnfolderButton").gameObject.SetActive(true);
            transform.Find("FolderButton").gameObject.SetActive(false);
        });

        transform.Find("UnfolderButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            transform.Find("Main").gameObject.SetActive(true);
            transform.Find("UnfolderButton").gameObject.SetActive(false);
            transform.Find("FolderButton").gameObject.SetActive(true);
        });
    }

    public void UpdateModelBoard(string resourcePath)
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

        List<int> groups = GenerateList(MapEditorSystem.instance.groupCount);
        Utils.FillContent(sectionContainer, sectionPrefab, groups, (GameObject section, int group) =>
        {
            List<int> items = GenerateList(MapEditorSystem.instance.GetItemsCount(group));
            GameObject content = section.GetComponent<UICollapsibleView>().content.gameObject;
            Utils.FillContent(content, draggabeItemPrefab, items, (GameObject go, int itemIndex) =>
            {
                var tmpAsset = MapEditorSystem.instance.GetItem(group, itemIndex);
                go.transform.Find("Name").GetComponent<TMP_Text>().text = tmpAsset.name;
                go.transform.Find("Icon").GetComponent<Image>().sprite = Utils.LoadSpriteFromFile(Path.Combine(resourcePath, tmpAsset.icon), new Vector2(0.5f, 0.5f));
                go.SetActive(true);

                go.transform.Find("Interact").GetComponent<Button>().onClick.AddListener(() =>
                {
                    MapEditorSystem.instance.PickUp(group, itemIndex);
                });

                //var loadingBtn = go.transform.Find("Loading").GetComponent<Button>();
                //loadingBtn.onClick.AddListener(() =>
                //{
                //    loadingBtn.interactable = false;
                //    loadingBtn.GetComponentInChildren<TMP_Text>().text = "Мгдижа";
                //    MapEditorSystem.instance.LoadAsset(group, itemIndex, () =>
                //    {
                //        loadingBtn.gameObject.SetActive(false);
                //    });
                //});
            }, false);

            var view = section.GetComponent<UICollapsibleView>();
            view.head = MapEditorSystem.instance.GetGroupName(group);
            view.Resize();

        });
    }

    public void HideLoadingMask()
    {
        transform.Find("Main/LoadingMask").gameObject.SetActive(false);
    }
}
