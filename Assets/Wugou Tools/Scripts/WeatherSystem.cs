using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 天气描述 
    /// </summary>
    public struct WeatherDesc
    {
        public int type;
        public float time;
        public float fogDensity;
        public float windSpeed;
        public float windDir;
    }

    public class WeatherSystem
    {
        /// <summary>
        /// 当前天气类型
        /// </summary>
        public static WeatherDesc activeWeather { get; set; }

        /// <summary>
        /// 所有天气类型名字
        /// </summary>
        public static List<string> allWeatherNames { get; private set; } = new List<string>();

        /// <summary>
        /// 加载天气插件物体
        /// </summary>
        public static System.Action Load = () =>
        {
            throw new System.Exception("Unimplement WeatherSystem's Load function.");
        };

        /// <summary>
        /// 应用天气，需要根据使用的天气系统来决定实现
        /// </summary>
        public static System.Action ApplyWeather = () =>
        {
            throw new System.Exception("Unimplement WeatherSystem's ApplyWeather function.");
        };

        /// <summary>
        /// 清理天气插件内容
        /// </summary>
        public static System.Action Clear = () =>
        {

        };
    }
}
