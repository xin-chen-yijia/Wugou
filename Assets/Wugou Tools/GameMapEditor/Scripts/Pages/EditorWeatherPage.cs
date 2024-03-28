using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Wugou.Editor;
using Wugou.UI; 
using UnityEngine.Events;
using System;

namespace Wugou.Editor.UI {
    public class EditorWeatherPage : UIBaseWindow
    {
        public Button okButton;
        public Button closeButton;

        public TMP_Dropdown weatherTypeDropdown;
        public Slider timeSlider;
        public Slider fogSlider;
        public TMP_Dropdown windForceDropdown;

        private void Awake()
        {
            GameMapEditor.onLoadedMap.AddListener(() =>
            {
                SetOptions(WeatherSystem.allWeatherNames);

                var weather = WeatherSystem.activeWeather;
                Logger.DebugInfo($"weather time:{weather.time}");
                timeSlider.SetValueWithoutNotify(weather.time);
                weatherTypeDropdown.SetValueWithoutNotify(weather.type);
                fogSlider.SetValueWithoutNotify(weather.fogDensity);
            });
        }

        // Start is called before the first frame update
        async void Start()
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
                GameMapEditor.instance.loadedGameMap.weather.type = index;
                var weather = WeatherSystem.activeWeather;
                weather.type = index;
                WeatherSystem.activeWeather = weather;
                WeatherSystem.ApplyWeather();
            });

            timeSlider.onValueChanged.AddListener((float value) =>
            {
                GameMapEditor.instance.loadedGameMap.weather.time = value;
                var weather = WeatherSystem.activeWeather;
                weather.time = value;
                WeatherSystem.activeWeather = weather;
                WeatherSystem.ApplyWeather();
            });

            fogSlider.onValueChanged.AddListener((float value) =>
            {
                GameMapEditor.instance.loadedGameMap.weather.fogDensity = value;
                var weather = WeatherSystem.activeWeather;
                weather.fogDensity = value;
                WeatherSystem.activeWeather = weather;
                WeatherSystem.ApplyWeather();
            });

            windForceDropdown.onValueChanged.AddListener((value) =>
            {
                float force = (value + 1) * 1;
                GameMapEditor.instance.loadedGameMap.weather.windSpeed= force;    // 记录，用于保存
                var weather = WeatherSystem.activeWeather;
                weather.windSpeed = force;
                WeatherSystem.activeWeather = weather;
                WeatherSystem.ApplyWeather();
            });

            //transform.Find("Main/WindDir/Value").GetComponent<Slider>().onValueChanged.AddListener((float value) =>
            //{
            //    MapEditorSystem.instance.loadedGameMap.weather.windDir = value;
            //    var weather = WeatherSystem.activeWeather;
            //    weather.windDir = value;
            //    WeatherSystem.activeWeather = weather;
            //    WeatherSystem.ApplyWeather();
            //});
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
