using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;

namespace Wugou
{
    /// <summary>
    /// 四元数序列化
    /// </summary>
    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteValue($"{value.x},{value.y},{value.z},{value.w}");
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            string ss = (string)reader.Value;
            string[] parts = ss.Split(',');
            Debug.Assert(parts.Length == 4);

            return new Quaternion(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
        }
    }
}