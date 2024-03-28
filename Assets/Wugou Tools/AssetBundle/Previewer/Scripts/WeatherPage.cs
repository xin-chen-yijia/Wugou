using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Wugou.UI;
using Wugou;
using System;

namespace Wugou.Examples.AssetbundlePreviewer
{ 
    public class WeatherPage : UIBaseWindow
    {
        public Slider timeSlider;
        public Dropdown weatherDropdown;
        // Start is called before the first frame update
        void Start()
        {
            // 
            List<Dropdown.OptionData> weatherValues = new List<Dropdown.OptionData>();
            foreach(var data in WeatherSystem.allWeatherNames)
            {
                weatherValues.Add(new Dropdown.OptionData(data));
            }
            weatherDropdown.options = weatherValues;
            weatherDropdown.onValueChanged.AddListener((index) =>
            {
                var tmp = WeatherSystem.activeWeather;
                tmp.type = index;
                WeatherSystem.activeWeather = tmp;
                WeatherSystem.ApplyWeather();
            });

            timeSlider.maxValue = 0.995f;
            timeSlider.onValueChanged.AddListener((value) =>
            {
                var tmp = WeatherSystem.activeWeather;
                tmp.time = value;
                WeatherSystem.activeWeather = tmp;
                WeatherSystem.ApplyWeather();
            });

        }
    
        // Update is called once per frame
        //void Update()
        //{
            
        //}
    }
}
