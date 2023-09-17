using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.MapEditor
{
    public class FloatPropertyView : PropertyView<float>
    {
        public override float ParseFromString(string value)
        {
            float t = 0;
            if (float.TryParse(value, out t))
            {
                return t;
            }

            return value_;
        }
    }
}
