using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;

namespace Wugou
{
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