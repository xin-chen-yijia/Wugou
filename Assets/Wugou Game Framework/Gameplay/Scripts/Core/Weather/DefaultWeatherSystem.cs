using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    /// <summary>
    /// 空天气系统，什么都不干
    /// </summary>
    public class DefaultWeatherSystem : WeatherSystemBase
    {
        public override float time { get; set; }

        public override bool fogEnable { get; set; }
        public override float fogDensity { get; set; }

        public override float windDirection { get; set; }

        public override float windForce { get; set; }


        public override void ApplyWeatherEffectsToPlayer(GameObject player)
        {
            Logger.DebugInfo("DefaultWeatherSystem ApplyWeatherEffectsToPlayer.");
        }

        public override void SetWeatherTransition(bool value)
        {
            Logger.DebugInfo($"DefaultWeatherSystem SetWeatherTransition {value}.");
        }
    }
}

