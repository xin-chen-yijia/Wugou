using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Wugou;
using System.IO;
using UnityEngine.Events;
using TMPro;
using System.Linq;
using Wugou.MapEditor;

namespace Wugou.UI
{
    public class GameMapTemplatePage : UIBaseWindow
    {
        public GameObject itemPrefab;
        public GameObject itemContainer;

        public UnityAction<AssetBundleScene> onStartNewGameMap;

        // Start is called before the first frame update
        //void Start()
        //{
        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public void SetContent(List<string> content)
        {
            Wugou.Utils.FillContent<string>(itemContainer, itemPrefab, content, (obj, templateName) =>
            {
                obj.transform.Find("Name").GetComponent<TMP_Text>().text = templateName;
                obj.GetComponentInChildren<Button>().onClick.AddListener(() =>
                {
                    GameMapTemplate template = null;
                    // 
                    var allCards = GameMapManager.assetbundleSceneCards;
                    var filteredCards = allCards;
                    if (templateName != "Empty")
                    {
                        template = GameMapManager.GetGameMapTemplate(templateName);
                        filteredCards = allCards.FindAll(card => { return template.sceneTags.Any(t => card.tags.Any(b => t == b)); });
                    }

                    var page = rootWindow.GetChildWindow<SceneSelectPage>();
                    page.SetScenes(filteredCards);
                    page.onSelectScene = (scene)=>
                    {
                        Logger.Info("New GameMap with tempalte:" + templateName + " and scene:" +scene.sceneName);
                        var map = GameMapManager.CreateGameMapFromTemplate(template);
                        map.scene = scene;
                        MapEditorSystem.StartEditor(map);
                        Hide();
                    };
                    page.Show();

                    Hide();
                });
            });
        }

        public void OnBackClick()
        {
            rootWindow.GetChildWindow<GameMapListPage>().Show();
            Hide();
        }
    }

}
