using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        public float windForce;
        public float windDir;
    }

    public interface IWeatherSystem
    {
        /// <summary>
        /// 当前天气类型
        /// </summary>
        public int weatherType { get;}

        /// <summary>
        /// 修改天气类型
        /// </summary>
        /// <param name="weatherType"></param>
        /// <param name="transition">天气是否渐变过渡</param>
        public void ChangeWeather(int weatherType, bool transition = false);

        /// <summary>
        /// 所有天气类型名字
        /// </summary>
        public abstract IList<string> GetAllWeatherNames();

        /// <summary>
        /// 天气系统是否已加载
        /// </summary>
        public abstract bool isLoaded { get; }

        /// <summary>
        /// 加载天气插件物体
        /// </summary>
        public abstract void Load();

        /// <summary>
        /// 模拟时间
        /// </summary>
        /// <param name="time">当天的时间换算成秒</param>
        public abstract float time { get; set; }

        public abstract int year { get; set; }
        public abstract int month { get; set; }
        public abstract int day { get; set; }
        public abstract int hour { get; set; }
        public abstract int minute { get; set; }


        /// <summary>
        /// 设置日期和时间
        /// </summary>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="hour"></param>
        /// <param name="minute"></param>
        /// <param name="second"></param>
        public abstract void SetTime(int year, int month, int day, int hour, int minute, float second);

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
        /// 清理天气插件内容
        /// </summary>
        public abstract void Clear();
    }
}
