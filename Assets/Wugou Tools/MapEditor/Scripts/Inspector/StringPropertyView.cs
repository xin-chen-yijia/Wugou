using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.MapEditor
{
    public class StringPropertyView : PropertyView
    {
        public override void ParseFromString(string value)
        {
            value_ = value;
        }
    }
}
