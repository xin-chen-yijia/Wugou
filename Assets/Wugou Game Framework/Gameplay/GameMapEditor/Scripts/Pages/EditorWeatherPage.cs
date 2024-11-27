using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wugou.UI; 

namespace Wugou.Editor.UI {
    public class EditorWeatherPage : UIBaseWindow
    {
        public TMP_Dropdown weatherTypeDropdown;
        public Slider timeSlider;
        public Toggle fogToggle;
        public Slider fogSlider;
        public TMP_Dropdown windForceDropdown;
        public TMP_Dropdown windDirDropdown;
        public Slider volumeSlider;

        //public override void Awake()
        //{
        //    base.Awake();

        //    GameMapEditor.instance.onLoadedMap.AddListener(async () =>
        //    {
        //        if (GameWorld.loadedMap.needWeather)
        //        {
        //            var weatherSys = Gameplay.weatherSystem;

        //            // 禁用天气自动变换
        //            weatherSys.SetWeatherTransition(false);

        //            SetOptions(new List<string>(weatherSys.GetAllWeatherNames()));

        //            await new EnumeratorAwaiter(new WaitUntil(() => { return weatherSys.isLoaded; }));

        //            timeSlider.SetValueWithoutNotify(weatherSys.time);
        //            weatherTypeDropdown.SetValueWithoutNotify(weatherSys.weatherType);
        //            fogSlider.SetValueWithoutNotify(weatherSys.fogDensity);
        //            fogToggle.SetIsOnWithoutNotify(true);
        //            windForceDropdown.SetValueWithoutNotify(Mathf.RoundToInt(windForceDropdown.options.Count * weatherSys.windForce) - 1);
        //            windDirDropdown.SetValueWithoutNotify(Mathf.RoundToInt(windDirDropdown.options.Count * weatherSys.windDirection));

        //            volumeSlider.SetValueWithoutNotify(GameSoundsManager.activeHandler.GetWeatherVolume());
        //        }
        //        else
        //        {
        //            Hide();
        //        }

        //    });
        //}

        // Start is called before the first frame update
        void Start()
        {
            // 应用当前值
            Utils.DoAsync(async () =>
            {
                if (GameWorld.loadedMap.needWeather)
                {
                    var weatherSys = GameWorld.weatherSystem;

                    // 禁用天气自动变换
                    weatherSys.SetWeatherTransition(false);

                    SetOptions(new List<string>(weatherSys.GetAllWeatherNames()));

                    await new EnumeratorAwaiter(new WaitUntil(() => { return weatherSys.isLoaded; }));

                    timeSlider.SetValueWithoutNotify(weatherSys.time);
                    weatherTypeDropdown.SetValueWithoutNotify(weatherSys.weatherType);
                    fogSlider.SetValueWithoutNotify(weatherSys.fogDensity);
                    fogToggle.SetIsOnWithoutNotify(true);
                    windForceDropdown.SetValueWithoutNotify(Mathf.RoundToInt(windForceDropdown.options.Count * weatherSys.windForce) - 1);
                    windDirDropdown.SetValueWithoutNotify(Mathf.RoundToInt(windDirDropdown.options.Count * weatherSys.windDirection));

                    volumeSlider.SetValueWithoutNotify(GameSoundsManager.activeHandler.GetWeatherVolume());
                }
                else
                {
                    Hide();
                }
            });


            weatherTypeDropdown.onValueChanged.AddListener((int index) =>
            {
                var weatherSys = GameWorld.weatherSystem;
                weatherSys.ChangeWeather(index, false);

                // 因为weather是一个综合性的参数，包括了fog、wind这些东西，所以要重新改回来
                timeSlider.SetValueWithoutNotify(weatherSys.time);
                fogSlider.SetValueWithoutNotify(weatherSys.fogDensity);
                fogToggle.SetIsOnWithoutNotify(true);
                windForceDropdown.SetValueWithoutNotify(Mathf.RoundToInt(windForceDropdown.options.Count * weatherSys.windForce) - 1);
                windDirDropdown.SetValueWithoutNotify(Mathf.RoundToInt(windDirDropdown.options.Count * weatherSys.windDirection));
            });

            timeSlider.onValueChanged.AddListener((float value) =>
            {
                GameWorld.weatherSystem.time = (value);
            });

            fogToggle.onValueChanged.AddListener((val) =>
            {
                GameWorld.weatherSystem.fogEnable = val;
            });

            var lastFogVal = fogSlider.value;
            fogSlider.onValueChanged.AddListener((float value) =>
            {
                GameWorld.weatherSystem.fogDensity = (value);
            });

            windForceDropdown.onValueChanged.AddListener((value) =>
            {
                float force = (value + 1) * 1.0f / windForceDropdown.options.Count;
                GameWorld.weatherSystem.windForce = (force);
            });

            windDirDropdown.onValueChanged.AddListener((value) =>
            {
                var dirVal = value * 1.0f / (windDirDropdown.options.Count);
                GameWorld.weatherSystem.windDirection = (dirVal);
            });

            volumeSlider.onValueChanged.AddListener((value) =>
            {
                GameSoundsManager.activeHandler.SetWeatherVolume(value);
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
