using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;

namespace Wugou
{
    /// <summary>
    /// Vector3 内部属性有返回Vector3的情况，序列化会陷入无限循环的状态
    /// </summary>
    public class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            //writer.WriteValue($"{value.r},{value.g},{value.b},{value.a}");
            writer.WriteValue(ColorUtility.ToHtmlStringRGBA(value));
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            //string ss = (string)reader.Value;
            //string[] parts = ss.Split(',');
            //Debug.Assert(parts.Length == 4);

            //return new Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));

            string val = (string)reader.Value;
            if (ColorUtility.TryParseHtmlString($"#{val}", out Color c)) // !!! ColorUtility.ToHtmlStringRGBA 竟然不加#。。。。
            {
                return c;
            }
            else
            {
                Logger.Error($"Parse {val} to color fail..");
                return Color.white;
            }
        }
    }
}