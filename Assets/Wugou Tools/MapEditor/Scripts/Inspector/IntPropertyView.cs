using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.MapEditor
{
    public class IntPropertyView : PropertyView<int>
    {
        public override int ParseFromString(string value)
        {
            int t = 0;
            if (int.TryParse(value, out t))
            {
                return t;
            }

            return value_;
        }
    }
}
