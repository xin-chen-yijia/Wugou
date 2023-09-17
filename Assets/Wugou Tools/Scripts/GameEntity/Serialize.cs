using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wugou.MapEditor;
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
    /// AssetBundleDesc 序列化
    /// 采用引用的方式，避免每个地方都是AssetBundle的全部内容
    /// </summary>
    public class AssetBundleDescHashConvert : JsonConverter<AssetBundleDesc>
    {
        private Dictionary<int, AssetBundleDesc> assetBundles_;
        public List<AssetBundleDesc> assetbundleDescs => assetBundles_.Values.ToList();

        public AssetBundleDescHashConvert(Dictionary<int, AssetBundleDesc> descriptions)
        {
            assetBundles_ = descriptions;
        }

        public override void WriteJson(JsonWriter writer, AssetBundleDesc value, JsonSerializer serializer)
        {
            if(!assetBundles_.ContainsKey(value.id)) 
            { 
                assetBundles_.Add(value.id, value);
            }

            writer.WriteValue(value.id);
        }

        public override AssetBundleDesc ReadJson(JsonReader reader, Type objectType, AssetBundleDesc existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            int id = Convert.ToInt32(reader.Value);
            if(assetBundles_.ContainsKey(id))
            {
                return assetBundles_[id];
            }
            else
            {
                Debug.LogError($"Can't find assetbundle with id:{id}");
                return assetBundles_.First().Value;
            }
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
                // 注意：这里list是合并而非替换
                JsonConvert.PopulateObject(jo[comp.GetType().Name].ToString(), comp, settings);
            }

            return entity;
        }

        public override void WriteJson(JsonWriter writer, GameEntity value, JsonSerializer serializer)
        {
            JObject jo = new JObject();
            foreach (var v in value.GetComponents<GameComponent>())
            {
                JObject comJo = new JObject();
                Type t = v.GetType();
                foreach(var field in t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    comJo.Add(field.Name, JToken.FromObject(field.GetValue(v), serializer));
                }

                // property and serialize field
                foreach(var field in t.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if(field.GetCustomAttribute<SerializeField>() != null)
                    {
                        comJo.Add(field.Name, JToken.FromObject(field.GetValue(v), serializer));
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

