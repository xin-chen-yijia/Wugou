using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wugou.MapEditor
{
    public class IntPropertyView : PropertyView
    {
        public override void ParseFromString(string value)
        {
            int t = 0;
            if (int.TryParse(value, out t))
            {
                value_ = t;
            }
        }
    }
}
