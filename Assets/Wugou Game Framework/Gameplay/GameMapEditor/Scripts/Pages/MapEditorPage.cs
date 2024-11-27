using UnityEngine;
using UnityEngine.UI;
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
        public Toggle weatherToggle;
        public Toggle settingToggle;
        public Toggle previewToggle;
        public Button saveButton;
        public Button undoButton;
        public Button redoButton;
        public Button delButton;

        public TMP_Dropdown editorSpaceDropdown;
        public Toggle forwardNormalToggle;
        public Toggle perspectiveToggle;

        private GameEntity selectedEntity_ = null;

        // Start is called before the first frame update
        void Start()
        {
            GameMapEditor.instance.onOptionModeChanged.AddListener((mode) =>
            {
                //
                switch (mode)
                {
                    case GameMapEditor.OptionMode.kView:
                        transform.Find("Options/Options/View").GetComponent<Toggle>().SetIsOnWithoutNotify(true);
                        break;
                    case GameMapEditor.OptionMode.kTranslate:
                        transform.Find("Options/Options/Move").GetComponent<Toggle>().SetIsOnWithoutNotify(true);
                        break;
                    case GameMapEditor.OptionMode.kRotate:
                        transform.Find("Options/Options/Rotate").GetComponent<Toggle>().SetIsOnWithoutNotify(true);
                        break;
                    case GameMapEditor.OptionMode.kScale:
                        transform.Find("Options/Options/Scale").GetComponent<Toggle>().SetIsOnWithoutNotify(true);
                        break;
                    default:
                        break;
                }
            });

            GameMapEditor.instance.onSelectGameEntity.AddListener((entity) =>
            {
                selectedEntity_ = entity;
                delButton.interactable = selectedEntity_ != null;
            });

            GameMapEditor.instance.onLoadedMap.AddListener(() =>
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

            weatherToggle.onValueChanged.AddListener((val) =>
            {
                if (val)
                {
                    rootWindow.GetChildWindow<EditorWeatherPage>().Show();
                }
                else
                {
                    rootWindow.GetChildWindow<EditorWeatherPage>().Hide();
                }
            });

            settingToggle.onValueChanged.AddListener((val) =>
            {
                if (val) { rootWindow.GetChildWindow<EditorSettingPage>().Show(); }
                else { rootWindow.GetChildWindow<EditorSettingPage>().Hide(); }
            });

            previewToggle.onValueChanged.AddListener((value) =>
            {
                GameMapEditor.instance.SwitchPreviewMode(value);
            });

            editorSpaceDropdown.onValueChanged.AddListener((value) =>
            {
                GameMapEditor.instance.editorAxis.space = value == 0     ? Space.World : Space.Self;
            });

            forwardNormalToggle.onValueChanged.AddListener((value) =>
            {
                GameMapEditor.instance.isPlaceGameEntityForwardHitNormal = value;
            });

            perspectiveToggle.onValueChanged.AddListener((value) =>
            {
                GameMapEditor.instance.SetOrtho(value);
            });

            saveButton.onClick.AddListener(() =>
            {
                GameMapEditor.instance.Save();
            });

            undoButton.onClick.AddListener(() =>
            {
                GameMapEditor.instance.Undo.Undo();
            });

            redoButton.onClick.AddListener(() =>
            {
                GameMapEditor.instance.Undo.Redo();
            });

            delButton.onClick.AddListener(() =>
            {
                if (selectedEntity_)
                {
                    GameMapEditor.instance.DestroyGameEntityAndRecord(selectedEntity_);
                }
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
                GameMapEditor.instance.SetOptionMode(GameMapEditor.OptionMode.kView);
            }
        }

        public void OnMoveOptionToggle(bool isOn)
        {
            if (isOn)
            {
                GameMapEditor.instance.SetOptionMode(GameMapEditor.OptionMode.kTranslate);
            }
        }

        public void OnRotateOptionToggle(bool isOn)
        {
            if (isOn)
            {
                GameMapEditor.instance.SetOptionMode(GameMapEditor.OptionMode.kRotate);
            }
        }

        public void OnScaleOptionToggle(bool isOn)
        {
            if (isOn)
            {
                GameMapEditor.instance.SetOptionMode(GameMapEditor.OptionMode.kScale);
            }
        }

        public void OnGroundOptionToggle(bool isOn)
        {
            if (isOn)
            {
                GameMapEditor.instance.SetOptionMode(GameMapEditor.OptionMode.kAttach);
            }
        }
    }
}
