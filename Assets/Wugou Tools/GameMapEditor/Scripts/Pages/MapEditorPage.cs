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
        public TMP_Text headText;
        public Button quitButton;
        public Button buildButton;
        public Toggle previewToggle;

        public Toggle editorSpaceToggle;
        public Toggle forwardNormalToggle;
        public Toggle perspectiveToggle;

        // Start is called before the first frame update
        void Start()
        {
            GameMapEditor.instance?.editorAxis.onOptionModeChanged.AddListener((mode) =>
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

            previewToggle.onValueChanged.AddListener((value) =>
            {
                GameMapEditor.instance.SwitchPreviewMode(value);
            });

            editorSpaceToggle.onValueChanged.AddListener((value) =>
            {
                GameMapEditor.instance.editorAxis.space = value ? Space.World : Space.Self;
            });

            forwardNormalToggle.onValueChanged.AddListener((value) =>
            {
                GameMapEditor.instance.placeGameEntityForwardHitNormal = value;
            });

            perspectiveToggle.onValueChanged.AddListener((value) =>
            {
                GameMapEditor.instance.SetOrtho(value);
            });
        }

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public void SetHead(string head)
        {
            headText.text = head;
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
