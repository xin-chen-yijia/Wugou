using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.IO;

namespace Wugou
{
    public static class JsonSerializerGlobal
    {
        public static JsonConverter[] commonConverts = new JsonConverter[] { new GameEntityConverter(), new Vector3Converter(), new QuaternionConverter() };
        //public static JsonConverterCollection commonConverts = new JsonConverterCollection() { new GameEntityConverter(), new Vector3Converter(), new QuaternionConverter()};

        public static JsonSerializer commonSerializer = JsonSerializer.Create(new JsonSerializerSettings() { Converters = JsonSerializerGlobal.commonConverts, ContractResolver= new LimitRefContractResolver(),NullValueHandling = NullValueHandling.Ignore });

        public static HashSet<Type> limitTypes { get; } = new HashSet<Type>() {
            typeof(GameObject),
            typeof(Transform),
            typeof(Collider),
            typeof(Rigidbody),
        };
    }

    /// <summary>
    /// GameEntity 序列化和反序列化：
    /// 1. 序列化GameEntity自身和其上的带Serializable的组件；
    /// 2. 只有SerializeField的属性才序列化
    /// 3. 序列化使用JsonSerializerGlobal.commonSerializer
    /// </summary>
    public class GameEntityConverter: JsonConverter<GameEntity>
    {
        public override GameEntity ReadJson(JsonReader reader, Type objectType, GameEntity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jo = JObject.ReadFrom(reader);
            string asset = jo[nameof(GameEntity)]["asset"].ToString();
            string prototype = jo[nameof(GameEntity)]["prototype"].ToString();
            GameEntity entity = GameEntityManager.CreateGameEntity(asset, prototype);

            foreach (var comp in entity.GetComponents<MonoBehaviour>())
            {
                var compType = comp.GetType();
                if (compType.GetCustomAttribute<System.SerializableAttribute>() == null)
                {
                    continue;
                }
                // 处理list的问题，PopulateObject是追加而非替换
                foreach (var field in compType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<SerializeField>() != null && Utils.IsListOrArray(field.FieldType))
                    {
                        var clearMethod = field.FieldType.GetMethod("Clear");
                        clearMethod.Invoke(field.GetValue(comp), new object[] { });
                    }
                }

                foreach (var field in compType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<SerializeField>() != null && Utils.IsListOrArray(field.PropertyType))
                    {
                        var clearMethod = field.PropertyType.GetMethod("Clear");
                        clearMethod.Invoke(field.GetValue(comp), new object[] { });
                    }
                }

                if (jo[compType.Name] != null)
                {
                    PopulateObject(jo[compType.Name].ToString(), comp, serializer);
                }

            }

            return entity;
        }

        private void PopulateObject(string value, object target, JsonSerializer jsonSerializer)
        {
            using JsonReader jsonReader = new JsonTextReader(new StringReader(value));
            jsonSerializer.Populate(jsonReader, target);

            while (jsonReader.Read())
            {
                if (jsonReader.TokenType != JsonToken.Comment)
                {
                    throw new System.Exception("Additional text found in JSON string after finishing deserializing object.");
                }
            }
        }

        public override void WriteJson(JsonWriter writer, GameEntity value, JsonSerializer serializer)
        {
            JObject jo = new JObject();
            foreach (var comp in value.GetComponents<MonoBehaviour>())
            {
                Type compType = comp.GetType();
                if (compType.GetCustomAttribute<System.SerializableAttribute>() == null)
                {
                    continue;
                }
                JObject comJo = new JObject();
                foreach(var field in compType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if(field.GetCustomAttribute<SerializeField>() != null)
                    {
                        var val = field.GetValue(comp);
                        if (val != null)
                        {
                            comJo.Add(field.Name, JToken.FromObject(val, serializer));
                        }
                    }
                }

                // property and serialize field
                foreach(var property in compType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if(property.GetCustomAttribute<SerializeField>() != null)
                    {
                        comJo.Add(property.Name, JToken.FromObject(property.GetValue(comp), serializer));
                    }
                }

                jo.Add(compType.Name, comJo);
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

    public class LimitRefContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> list = base.CreateProperties(type, memberSerialization); //只保留清单有列出的属性
            return list.Where(p =>
            {
                return !JsonSerializerGlobal.limitTypes.Contains(p.PropertyType);
            }).ToList();
        }
    }
}

