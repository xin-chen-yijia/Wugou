using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// �������� 
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
        /// ��ǰ��������
        /// </summary>
        public static WeatherDesc activeWeather { get; set; }

        /// <summary>
        /// ����������������
        /// </summary>
        public static List<string> allWeatherNames { get; private set; } = new List<string>();

        /// <summary>
        /// ���������������
        /// </summary>
        public static System.Action Load = () =>
        {
            throw new System.Exception("Unimplement WeatherSystem's Load function.");
        };

        /// <summary>
        /// Ӧ����������Ҫ����ʹ�õ�����ϵͳ������ʵ��
        /// </summary>
        public static System.Action ApplyWeather = () =>
        {
            throw new System.Exception("Unimplement WeatherSystem's ApplyWeather function.");
        };

        /// <summary>
        /// ���������������
        /// </summary>
        public static System.Action Clear = () =>
        {

        };
    }
}
