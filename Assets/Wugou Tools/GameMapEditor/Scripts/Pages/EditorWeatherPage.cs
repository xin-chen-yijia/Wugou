using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wugou.UI; 

namespace Wugou.Editor.UI {
    public class EditorWeatherPage : UIBaseWindow
    {
        public Button okButton;
        public Button closeButton;

        public TMP_Dropdown weatherTypeDropdown;
        public Slider timeSlider;
        public Slider fogSlider;
        public TMP_Dropdown windForceDropdown;
        public TMP_Dropdown windDirDropdown;

        private void Awake()
        {
            GameMapEditor.onLoadedMap.AddListener(() =>
            {
                if (GameWorld.loadedMap.needWeather)
                {
                    SetOptions(new List<string>(Gameplay.weatherSystem.GetAllWeatherNames()));

                    var weatherSys = Gameplay.weatherSystem;
                    Logger.DebugInfo($"weather time:{weatherSys.time}");
                    timeSlider.SetValueWithoutNotify(weatherSys.time);
                    weatherTypeDropdown.SetValueWithoutNotify(weatherSys.weatherType);
                    fogSlider.SetValueWithoutNotify(weatherSys.fogDensity);
                    windForceDropdown.SetValueWithoutNotify(Mathf.RoundToInt(windForceDropdown.options.Count * weatherSys.windForce) - 1);
                    windDirDropdown.SetValueWithoutNotify(Mathf.RoundToInt(windDirDropdown.options.Count * weatherSys.windDirection));
                }
                else
                {
                    Hide();
                }

            });
        }

        // Start is called before the first frame update
        void Start()
        {
            okButton.onClick.AddListener(() =>
            {
                Hide();
            });

            closeButton.onClick.AddListener(() =>
            {
                Hide();
            });

            weatherTypeDropdown.onValueChanged.AddListener((int index) =>
            {
                Gameplay.weatherSystem.ChangeWeather(index, false);
            });

            timeSlider.onValueChanged.AddListener((float value) =>
            {
                Gameplay.weatherSystem.time = (value);
            });

            fogSlider.onValueChanged.AddListener((float value) =>
            {
                Gameplay.weatherSystem.fogDensity = (value);
            });

            windForceDropdown.onValueChanged.AddListener((value) =>
            {
                float force = (value + 1) * 1.0f / windForceDropdown.options.Count;
                Gameplay.weatherSystem.windForce = (force);
            });

            windDirDropdown.onValueChanged.AddListener((value) =>
            {
                var dirVal = value * 1.0f / (windDirDropdown.options.Count);
                Gameplay.weatherSystem.windDirection = (dirVal);
            });
        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        public void SetOptions(List<string> weathers)
        {
            var ops = new List<TMP_Dropdown.OptionData>();
            foreach(var v in weathers)
            {
                ops.Add(new TMP_Dropdown.OptionData(v));
            }

            weatherTypeDropdown.options = ops;
        }
    }
}
