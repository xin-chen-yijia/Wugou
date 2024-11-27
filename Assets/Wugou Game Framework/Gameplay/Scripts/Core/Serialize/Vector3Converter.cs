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
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value.x},{value.y},{value.z}");
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string ss = (string)reader.Value;
            string[] parts = ss.Split(',');
            Debug.Assert(parts.Length == 3);

            return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
        }
    }
}