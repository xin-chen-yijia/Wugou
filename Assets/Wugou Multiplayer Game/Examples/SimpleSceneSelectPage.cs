using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Wugou;
using Wugou.UI;
using UnityEngine.Events;
using System.Linq;

namespace Wugou.Examples.UI
{
    public class SimpleSceneSelectPage : UIBaseWindow
    {
        public GameObject sceneItemPrefab;
        public GameObject sceneItemContainer;

        public Button backButton;
        public Button okButton;

        public UnityAction<string> onSelectScene = null;

        private string selectedScene_;

        Dictionary<UnityScene, GameObject> sceneCards_ = new Dictionary<UnityScene, GameObject>();
        Transform tagsParent => transform.Find("Main/Shifting/Tags");

        // Start is called before the first frame update
        void Start()
        {
            backButton.onClick.AddListener(() =>
            {
                rootWindow.GetChildWindow<SimpleGameMapListPage>().Show();
                Hide();
            });

            okButton.onClick.AddListener(() =>
            {
                onSelectScene?.Invoke(selectedScene_);
                Hide();
            });

            foreach(Transform v in tagsParent)
            {
                v.GetComponent<Toggle>().onValueChanged.AddListener((ison) =>
                {
                    UpdateShifting();
                });
            }
        }

        void UpdateShifting()
        {
            List<string> tags = new List<string>();

            foreach (Transform v in tagsParent)
            {
                if (v.GetComponent<Toggle>().isOn)
                {
                    tags.Add(v.GetComponentInChildren<TMP_Text>().text);
                }
            }

            foreach(var v in sceneCards_)
            {
                v.Value.SetActive(v.Key.tags.Any(tag => tags.Contains(tag)));
            }

        }

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public async void SetScenes(List<UnityScene> scenes)
        {
            sceneCards_.Clear();
            float clickTime = -1;
            GameObject lastCheckedObj = null;
            Utils.FillContent(sceneItemContainer, sceneItemPrefab, scenes, (go, card) =>
            {
                go.transform.Find("Name/Name").GetComponent<TMP_Text>().text = card.name;
                ReadAndAssignIcon(go.transform.Find("Icon").GetComponent<Image>(), card.thumbnail);
                var button = go.transform.Find("Button").GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    selectedScene_ = card.scene;
                    if(Time.realtimeSinceStartup - clickTime < Utils.doubleClickMaxInterval)
                    {
                        onSelectScene?.Invoke(selectedScene_);
                        Hide();
                        return;
                    }

                    clickTime = Time.realtimeSinceStartup;
                    lastCheckedObj?.SetActive(false);
                    lastCheckedObj = go.transform.Find("Checked").gameObject;
                    lastCheckedObj.SetActive(true);
                });

                go.transform.Find("Description").GetComponentInChildren<TMP_Text>().text = card.description;

                sceneCards_.Add(card, go);

                // 默认选择第一个
                if (!lastCheckedObj)
                {
                    button.onClick.Invoke();
                }
            });

            foreach (Transform v in tagsParent)
            {
                v.GetComponent<Toggle>().SetIsOnWithoutNotify(true);
            }

            // layout的通病，不能及时更新排列
            await new YieldInstructionAwaiter(null);
            _ = Utils.ResizeContainer(sceneItemContainer);

        }

        private async void ReadAndAssignIcon(Image img,string icon)
        {
            img.sprite = await GameAssetDatabase.GetAssetAsync<Sprite>(icon);
        }
    }

}
