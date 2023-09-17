using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.UI;
using TMPro;
using UnityEngine.UI;
using Wugou;
using System.IO;
using static Wugou.MapEditor.MapEditorSystem;
using UnityEngine.Events;

namespace Wugou.MapEditor
{
    public class MapEditorPage : UIBaseWindow
    {
        // Start is called before the first frame update
        //void Start()
        //{

        //}

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
