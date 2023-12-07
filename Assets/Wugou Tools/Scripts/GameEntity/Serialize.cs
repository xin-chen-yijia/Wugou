using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using Newtonsoft.Json.Converters;

namespace Wugou
{
    /// <summary>
    /// 用于指定不序列化的Field
    /// </summary>
    public class NonSerializeField : Attribute
    {

    }

    public static class JsonSerializerGlobal
    {
        public static JsonConverterCollection commonConverts = new JsonConverterCollection() { new GameEntityConverter(), new AssetBundleDescConvert(), new Vector3Converter() };

        public static JsonSerializer commonSerializer = JsonSerializer.Create(new JsonSerializerSettings() { Converters = JsonSerializerGlobal.commonConverts });
    }

    /// <summary>
    /// AssetBundleDesc 序列化
    /// 采用引用的方式，避免每个地方都是AssetBundle的全部内容
    /// </summary>
    public class AssetBundleDescConvert : JsonConverter<AssetBundleDesc>
    {
        public override void WriteJson(JsonWriter writer, AssetBundleDesc value, JsonSerializer serializer)
        {
            writer.WriteValue(value.path);
        }

        public override AssetBundleDesc ReadJson(JsonReader reader, Type objectType, AssetBundleDesc existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new AssetBundleDesc() { path = (string)reader.Value };
        }
    }

    /// <summary>
    /// GameEntity 序列化和反序列化
    /// </summary>
    public class GameEntityConverter: JsonConverter<GameEntity>
    {
        public override GameEntity ReadJson(JsonReader reader, Type objectType, GameEntity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jo = JObject.ReadFrom(reader);
            GameEntityBlueprint blueprint = jo[nameof(GameEntity)]["blueprint"].ToObject<GameEntityBlueprint>(serializer);
            GameEntity entity = GameEntityManager.CreateGameEntity(blueprint);
            var settings = new JsonSerializerSettings { Converters = serializer.Converters };
            foreach (var comp in entity.GetComponents<GameComponent>())
            {
                // 处理list的问题，PopulateObject是追加而非替换
                var compType = comp.GetType();
                foreach (var field in compType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<SerializeField>() != null && Utils.IsList(field.FieldType))
                    {
                        var clearMethod = field.FieldType.GetMethod("Clear");
                        clearMethod.Invoke(field.GetValue(comp), new object[] { });
                    }
                }

                foreach (var field in compType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<SerializeField>() != null && Utils.IsList(field.PropertyType))
                    {
                        var clearMethod = field.PropertyType.GetMethod("Clear");
                        clearMethod.Invoke(field.GetValue(comp), new object[] { });
                    }
                }

                JsonConvert.PopulateObject(jo[compType.Name].ToString(), comp, settings);

            }

            return entity;
        }

        public override void WriteJson(JsonWriter writer, GameEntity value, JsonSerializer serializer)
        {
            JObject jo = new JObject();
            foreach (var comp in value.GetComponents<GameComponent>())
            {
                JObject comJo = new JObject();
                Type t = comp.GetType();
                foreach(var field in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<NonSerializeField>() != null)
                    {
                        continue;
                    }
                    comJo.Add(field.Name, JToken.FromObject(field.GetValue(comp), serializer));
                }

                // property and serialize field
                foreach(var field in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if(field.GetCustomAttribute<SerializeField>() != null)
                    {
                        comJo.Add(field.Name, JToken.FromObject(field.GetValue(comp), serializer));
                    }
                }

                jo.Add(t.Name, comJo);
            }

            jo.WriteTo(writer);
        }
    }


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

