using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Wugou;
using UnityEngine.Events;
using Wugou.Multiplayer;
using System.Diagnostics;
using System.Linq;

namespace Wugou.UI
{
    public class SceneSelectPage : UIBaseWindow
    {
        public GameObject sceneItemPrefab;
        public GameObject sceneItemContainer;

        public UnityAction<AssetBundleScene> onSelectScene = null;

        private List<AssetBundleSceneCard> sceneCards_ = new List<AssetBundleSceneCard>();

        // Start is called before the first frame update
        void Start()
        {
            transform.Find("Buttons/BackButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                rootWindow.GetChildWindow<GameMapListPage>().Show();
                Hide();
            });

            transform.Find("Buttons/OkButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                Hide();
            });
        }

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public void SetScenes(List<AssetBundleSceneCard> scenes)
        {
            sceneCards_ = scenes; 

            Utils.FillContent(sceneItemContainer, sceneItemPrefab, scenes, (GameObject go, AssetBundleSceneCard face) =>
            {
                AssetBundleScene tmp = face.scene;
                go.transform.Find("Name/Name").GetComponent<TMP_Text>().text = face.name;
                go.transform.Find("Icon").GetComponent<Image>().sprite = Utils.LoadSpriteFromFile(System.IO.Path.Combine(GameMapManager.resourceDir, face.icon), new Vector2(0.5f, 0.5f));
                go.transform.Find("Button").GetComponent<Button>().onClick.AddListener(() =>
                {
                    onSelectScene?.Invoke(tmp);
                    Hide();
                });
            });
        }
    }

}
