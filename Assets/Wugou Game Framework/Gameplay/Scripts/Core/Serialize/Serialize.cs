using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public static class JsonSerializerGlobal
    {
        public static JsonConverter[] commonConverts = new JsonConverter[] { new GameEntityConverter(), new Vector3Converter(), new QuaternionConverter(), new ColorConverter() };
        //public static JsonConverterCollection commonConverts = new JsonConverterCollection() { new GameEntityConverter(), new Vector3Converter(), new QuaternionConverter()};
        public static JsonSerializerSettings commonSerializerSettings = new JsonSerializerSettings() { Converters = JsonSerializerGlobal.commonConverts, ContractResolver= new LimitRefContractResolver(),NullValueHandling = NullValueHandling.Ignore };

        public static JsonSerializer commonSerializer = JsonSerializer.Create(commonSerializerSettings);

        public static HashSet<Type> limitTypes { get; } = new HashSet<Type>() {
            typeof(GameObject),
            typeof(Transform),
            typeof(Collider),
            typeof(Rigidbody),
        };
    }
}

