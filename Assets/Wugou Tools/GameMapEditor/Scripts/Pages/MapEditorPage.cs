using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Wugou;
using Wugou.UI;

namespace Wugou.Editor.UI
{
    public class MapEditorPage : UIBaseWindow
    {
        public Button quitButton;
        public Button buildButton;
        public Button previewButton;
        public Button editorSpaceButton;

        // Start is called before the first frame update
        void Start()
        {
            GameMapEditor.instance.editorAxis.onOptionModeChanged.AddListener((mode) =>
            {
                //
                switch (mode)
                {
                    case EditorAxis.Mode.kTranslate:
                        transform.Find("Options/Options/Move").GetComponent<Toggle>().SetIsOnWithoutNotify(true);
                        break;
                    case EditorAxis.Mode.kRotate:
                        transform.Find("Options/Options/Rotate").GetComponent<Toggle>().SetIsOnWithoutNotify(true);
                        break;
                    case EditorAxis.Mode.kScale:
                        transform.Find("Options/Options/Scale").GetComponent<Toggle>().SetIsOnWithoutNotify(true);
                        break;
                    default:
                        break;
                }
            });

            GameMapEditor.onLoadedMap.AddListener(() =>
            {
                SetHead(GameMapEditor.instance.loadedGameMap.name);
            });

            buildButton.onClick.AddListener(() =>
            {
                GameMapEditor.instance.BuildGameMap();
            });

            quitButton.onClick.AddListener(() =>
            {
                GameMapEditor.instance.Quit();
            });

            bool isPreview = false;
            previewButton.onClick.AddListener(() =>
            {
                isPreview = !isPreview;
                //
                GameMapEditor.instance.SwitchPreviewMode(isPreview);
                previewButton.transform.Find("Checked")?.gameObject.SetActive(isPreview);
            });

            editorSpaceButton.onClick.AddListener(() =>
            {
                var space = GameMapEditor.instance.editorAxis.space;
                if(space == Space.World)
                {
                    space = Space.Self;
                }
                else
                {
                    space = Space.World;
                }

                GameMapEditor.instance.editorAxis.space = space;

                editorSpaceButton.GetComponentInChildren<TMP_Text>().text = space == Space.World ? "世界" : "本地";
            });

        }

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public void SetHead(string head)
        {
            transform.Find("Top/Head").GetComponent<TMP_Text>().text = head;
        }

        public void OnViewOptionToggle(bool isOn)
        {
            if (isOn)
            {
                GameMapEditor.instance.SwitchOptionMode(GameMapEditor.OptionModel.kView);
            }
        }

        public void OnMoveOptionToggle(bool isOn)
        {
            if (isOn)
            {
                GameMapEditor.instance.SwitchOptionMode(GameMapEditor.OptionModel.kTranslate);
            }
        }

        public void OnRotateOptionToggle(bool isOn)
        {
            if (isOn)
            {
                GameMapEditor.instance.SwitchOptionMode(GameMapEditor.OptionModel.kRotate);
            }
        }

        public void OnScaleOptionToggle(bool isOn)
        {
            if (isOn)
            {
                GameMapEditor.instance.SwitchOptionMode(GameMapEditor.OptionModel.kScale);
            }
        }

        public void OnGroundOptionToggle(bool isOn)
        {
            if (isOn)
            {
                GameMapEditor.instance.SwitchOptionMode(GameMapEditor.OptionModel.kAttach);
            }
        }
    }
}
