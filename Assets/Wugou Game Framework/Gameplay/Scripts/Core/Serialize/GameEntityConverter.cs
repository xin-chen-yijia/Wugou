using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Reflection;
using System.IO;

namespace Wugou
{
    /// <summary>
    /// GameEntity 序列化和反序列化：
    /// 1. 序列化GameEntity自身和其上的带Serializable的组件；
    /// 2. 只有SerializeField的属性才序列化
    /// 3. 序列化使用JsonSerializerGlobal.commonSerializer
    /// </summary>
    public class GameEntityConverter : JsonConverter<GameEntity>
    {
        public override GameEntity ReadJson(JsonReader reader, Type objectType, GameEntity existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JToken jo = JObject.ReadFrom(reader);
            string asset = jo[nameof(GameEntity)]["asset"]?.ToString();
            string prototype = jo[nameof(GameEntity)]["prototype"]?.ToString();

            GameEntity entity;
            if (jo[nameof(GameEntity)][nameof(GameEntity.isFromTheBeginning)] != null && jo[nameof(GameEntity)][nameof(GameEntity.isFromTheBeginning)].ToObject<bool>())
            {
                entity = GameWorld.GetGameEntityExistFromTheBeginning(jo[nameof(GameEntity)][nameof(GameEntity.id)].ToObject<int>());
            }
            else
            {
                entity = GameEntityManager.CreateGameEntity(prototype, asset);
            }

            foreach (var comp in entity.GetComponents<MonoBehaviour>())
            {
                var compType = comp.GetType();
                if (compType.GetCustomAttribute<EntitySerializableAttribute>() == null)
                {
                    continue;
                }
                // 处理list的问题，PopulateObject是追加而非替换
                foreach (var field in compType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<EntitySerializeFieldAttribute>() != null && Utils.IsListOrArray(field.FieldType))
                    {
                        if (field.FieldType.IsArray)
                        {
                            field.SetValue(comp, Array.CreateInstance(field.FieldType.GetElementType(), 0));
                        }
                        else
                        {
                            var clearMethod = field.FieldType.GetMethod("Clear");
                            if (clearMethod != null)
                            {
                                clearMethod.Invoke(field.GetValue(comp), new object[] { });
                            }
                        }

                    }
                }

                foreach (var field in compType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<EntitySerializeFieldAttribute>() != null && Utils.IsListOrArray(field.PropertyType))
                    {
                        if (field.PropertyType.IsArray)
                        {
                            field.SetValue(comp, Array.CreateInstance(field.PropertyType.GetElementType(), 0));
                        }
                        else
                        {
                            var clearMethod = field.PropertyType.GetMethod("Clear");
                            if (clearMethod != null)
                            {
                                clearMethod.Invoke(field.GetValue(comp), new object[] { });
                            }
                        }

                    }
                }

                if (jo[compType.Name] != null)
                {
                    // merge object content
                    PopulateObject(jo[compType.Name].ToString(), comp, serializer);

                    // 如果Rigidbody使用插值模式，会改变序列化时设置的transform信息
                    var rigidbody = entity.GetComponent<Rigidbody>();
                    if (rigidbody && rigidbody.interpolation != RigidbodyInterpolation.None)
                    {
                        rigidbody.position = entity.transform.position;
                        rigidbody.rotation = entity.transform.rotation;
                    }
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
                if (compType.GetCustomAttribute<EntitySerializableAttribute>() == null)
                {
                    continue;
                }
                JObject comJo = new JObject();
                foreach (var field in compType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (field.GetCustomAttribute<EntitySerializeFieldAttribute>() != null)
                    {
                        var val = field.GetValue(comp);
                        if (val != null)
                        {
                            comJo.Add(field.Name, JToken.FromObject(val, serializer));
                        }
                    }
                }

                // property and serialize field
                foreach (var property in compType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (property.GetCustomAttribute<EntitySerializeFieldAttribute>() != null)
                    {
                        var val = property.GetValue(comp);
                        if (val != null)
                        {
                            comJo.Add(property.Name, JToken.FromObject(val, serializer));
                        }
                    }
                }

                jo.Add(compType.Name, comJo);
            }

            jo.WriteTo(writer);
        }
    }


}
