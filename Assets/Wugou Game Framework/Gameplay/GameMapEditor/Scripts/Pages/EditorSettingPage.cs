using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Wugou.UI;
using TMPro;
using Wugou.Editor;

namespace Wugou.Editor.UI
{
    /// <summary>
    /// ±‡º≠∆˜…Ë÷√ΩÁ√Ê
    /// </summary>
    public class EditorSettingPage : UIBaseWindow
    {
        public TMP_Dropdown qualityDropdown;
        public Slider mouseMoveSlider;
        public Slider moveRotateSlider;

        // Start is called before the first frame update
        void Start()
        {
            string[] names = QualitySettings.names;
            var options = new List<TMP_Dropdown.OptionData>();
            for (int i = 0; i < names.Length; i++)
            {
                options.Add(new TMP_Dropdown.OptionData(names[i]));
            }
            qualityDropdown.options = options;
            qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
            qualityDropdown.onValueChanged.AddListener((val) =>
            {
                GameConsole.qualityLevel = val;
            });

            var flyCam = GameMapEditor.instance.flyCameraComp;
            float initMoveSpeed = flyCam.moveSpeed;
            mouseMoveSlider.onValueChanged.AddListener((val) =>
            {
                flyCam.moveSpeed = initMoveSpeed * val; 
            });

            float initXRotSpeed = flyCam.xRotSpeed;
            float initYRotSpeed = flyCam.yRotSpeed;
            moveRotateSlider.onValueChanged.AddListener((val) =>
            {
                flyCam.xRotSpeed = initXRotSpeed * val;
                flyCam.yRotSpeed = initYRotSpeed * val;
            });
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
