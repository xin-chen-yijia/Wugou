using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.MapEditor
{
    public class StringPropertyView : PropertyView<string>
    {
        public override string ParseFromString(string value)
        {
            return value;
        }
    }
}
