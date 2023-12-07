using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Wugou;
using Wugou.UI;

namespace Wugou.MapEditor.UI
{
    public class MapEditorPage : UIBaseWindow
    {
        // Start is called before the first frame update
        void Start()
        {
            MapEditorSystem.instance.editorAxis.onOptionModeChanged.AddListener((mode) =>
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
        }

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public void SetHead(string head)
        {
            transform.Find("Top/Head").GetComponent<TMP_Text>().text = head;
        }

        public void OnQuitClick()
        {
            MapEditorSystem.instance.Quit();
        }

        public void OnBasicInfoClick()
        {
            GetChildWindow<GameMapBasicInfoPage>().Show();
        }

        public void OnViewOptionToggle(bool isOn)
        {
            if (isOn)
            {
                MapEditorSystem.instance.SwitchOptionMode(MapEditorSystem.OptionModel.kView);
            }
        }

        public void OnMoveOptionToggle(bool isOn)
        {
            if (isOn)
            {
                MapEditorSystem.instance.SwitchOptionMode(MapEditorSystem.OptionModel.kTranslate);
            }
        }

        public void OnRotateOptionToggle(bool isOn)
        {
            if (isOn)
            {
                MapEditorSystem.instance.SwitchOptionMode(MapEditorSystem.OptionModel.kRotate);
            }
        }

        public void OnScaleOptionToggle(bool isOn)
        {
            if (isOn)
            {
                MapEditorSystem.instance.SwitchOptionMode(MapEditorSystem.OptionModel.kScale);
            }
        }

        public void OnGroundOptionToggle(bool isOn)
        {
            if (isOn)
            {
                MapEditorSystem.instance.SwitchOptionMode(MapEditorSystem.OptionModel.kAttach);
            }
        }
    }
}
