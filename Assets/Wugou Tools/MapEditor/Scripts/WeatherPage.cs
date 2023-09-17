using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.UI;
using UnityEngine.UI;
using TMPro;
using Wugou.MapEditor;
using UnityEngine.Events;
using System;

namespace Wugou.UI {
    public class WeatherPage : UIBaseWindow
    {
        // Start is called before the first frame update
        void Start()
        {
            transform.Find("Main/OkButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                Hide();
            });

            transform.Find("Main/WeatherType/Dropdown").GetComponent<TMP_Dropdown>().onValueChanged.AddListener((int index) =>
            {
                MapEditorSystem.instance.loadedGameMap.weather.type = index;
                var weather = WeatherSystem.activeWeather;
                weather.type = index;
                WeatherSystem.activeWeather = weather;
                WeatherSystem.ApplyWeather();
            });

            transform.Find("Main/Time/Value").GetComponent<Slider>().onValueChanged.AddListener((float value) =>
            {
                MapEditorSystem.instance.loadedGameMap.weather.time = value;
                var weather = WeatherSystem.activeWeather;
                weather.time = value;
                WeatherSystem.activeWeather = weather;
                WeatherSystem.ApplyWeather();
            });

            transform.Find("Main/Fog/Value").GetComponent<Slider>().onValueChanged.AddListener((float value) =>
            {
                MapEditorSystem.instance.loadedGameMap.weather.fogDensity = value;
                var weather = WeatherSystem.activeWeather;
                weather.fogDensity = value;
                WeatherSystem.activeWeather = weather;
                WeatherSystem.ApplyWeather();
            });

            transform.Find("Main/WindForce/Value").GetComponent<Slider>().onValueChanged.AddListener((float value) =>
            {
                MapEditorSystem.instance.loadedGameMap.weather.windSpeed= value;
                var weather = WeatherSystem.activeWeather;
                weather.windSpeed = value;
                WeatherSystem.activeWeather = weather;
                WeatherSystem.ApplyWeather();
            });

            transform.Find("Main/WindDir/Value").GetComponent<Slider>().onValueChanged.AddListener((float value) =>
            {
                MapEditorSystem.instance.loadedGameMap.weather.windDir = value;
                var weather = WeatherSystem.activeWeather;
                weather.windDir = value;
                WeatherSystem.activeWeather = weather;
                WeatherSystem.ApplyWeather();
            });
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void SetOptions(List<string> weathers)
        {
            var ops = new List<TMP_Dropdown.OptionData>();
            foreach(var v in weathers)
            {
                ops.Add(new TMP_Dropdown.OptionData(v));
            }

            var dropdown = transform.Find("Main/WeatherType").GetComponentInChildren<TMP_Dropdown>();
            dropdown.options = ops;
        }

        private bool inited = false;
        public override void Show(bool asTop = false)
        {
            if (!inited)
            {
                inited = true;
                SetOptions(WeatherSystem.allWeatherNames);
            }
            transform.Find("Main/Time/Value").GetComponent<Slider>().value = WeatherSystem.activeWeather.time;
            transform.Find("Main/WeatherType/Dropdown").GetComponent<TMP_Dropdown>().SetValueWithoutNotify(MapEditorSystem.instance.loadedGameMap.weather.type);
            transform.Find("Main/Fog/Value").GetComponent<Slider>().value = WeatherSystem.activeWeather.fogDensity;

            base.Show(asTop);
        }
    }
}
