using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 天气描述 
    /// </summary>
    public struct WeatherSetting
    {
        public int type;
        public float time;
        public float fogDensity;
        public float windForce;
        public float windDir;
    }

    public abstract class WeatherSystemBase
    {
        /// <summary>
        /// 当前天气类型
        /// </summary>
        public int weatherType { get; protected set; }

        /// <summary>
        /// 修改天气类型
        /// </summary>
        /// <param name="weatherType"></param>
        /// <param name="transition">天气是否渐变过渡</param>
        public void ChangeWeather(int weatherType, bool transition = false)
        {
            if (this.weatherType == weatherType)
            {
                return;
            }

            this.weatherType = weatherType;

            Debug.Assert(weatherType < GetAllWeatherNames().Count);
            LoadWeather(weatherType, transition);
        }

        /// <summary>
        /// 加载特定天气
        /// </summary>
        /// <param name="weatherType"></param>
        /// <param name="transition"></param>
        protected virtual void LoadWeather(int weatherType, bool transition = false)
        {

        }

        /// <summary>
        /// 所有天气类型名字
        /// </summary>
        public virtual List<string> GetAllWeatherNames()
        {
            return new List<string>();
        }

        /// <summary>
        /// 天气系统是否已加载
        /// </summary>
        public bool isLoaded { get; protected set; }

        /// <summary>
        /// 加载天气插件物体
        /// </summary>
        public virtual void Load(System.Action onLoaded = null)
        {
            isLoaded = true;
            onLoaded?.Invoke();
        }

        /// <summary>
        /// 清理天气插件内容
        /// </summary>
        public virtual void Unload()
        {
            isLoaded = false;
        }

        /// <summary>
        /// 模拟时间
        /// </summary>
        /// <param name="time">当天的时间换算成秒</param>
        public abstract float time { get; set; }

        public virtual int year { get; protected set; }
        public virtual int month { get; protected set; }
        public virtual int day { get; protected set; }
        public virtual int hour => (int)(time * 24);
        public virtual int minute => ((int)(time * 24 * 60)) % 60;
        public virtual int second => ((int)(time * 24 * 3600)) % 60;    // 可能有一秒的误差

        /// <summary>
        /// 设置日期
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        public virtual void SetDate(int year, int month, int day)
        {
            this.year = year;
            this.month = month;
            this.day = day;
        }

        /// <summary>
        /// 设置时间
        /// </summary>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        public void SetTime(int hour, int minute, int second)
        {
            time = (hour * 3600.0f + minute * 60 + second) / (24 * 3600);
        }

        /// <summary>
        /// 启用雾效
        /// </summary>
        /// <param name="enable"></param>
        public abstract bool fogEnable { get; set; }

        /// <summary>
        /// 设置雾的浓度
        /// </summary>
        /// <param name="density"></param>
        public abstract float fogDensity { get; set; }

        /// <summary>
        /// 设置风力
        /// </summary>
        /// <param name="windForce"></param>
        public abstract float windForce { get; set; }

        /// <summary>
        /// 设置风向
        /// </summary>
        /// <param name="direction"></param>
        public abstract float windDirection { get; set; }

        /// <summary>
        /// 把天气系统的一些特效，如雨雪等，让其跟随玩家角色
        /// </summary>
        /// <param name="player"></param>
        public abstract void ApplyWeatherEffectsToPlayer(GameObject player);

        /// <summary>
        /// 天气自动变换
        /// </summary>
        /// <param name="value"></param>
        public abstract void SetWeatherTransition(bool value);
    }
}
