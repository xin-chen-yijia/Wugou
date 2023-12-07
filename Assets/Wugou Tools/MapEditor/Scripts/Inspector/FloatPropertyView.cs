using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.MapEditor
{
    public class FloatPropertyView : PropertyView
    {
        public override void ParseFromString(string value)
        {
            float t = 0;
            if (float.TryParse(value, out t))
            {
                value_ = t;
            }

        }
    }
}
