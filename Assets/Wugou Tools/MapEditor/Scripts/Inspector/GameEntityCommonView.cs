using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Wugou.MapEditor
{
    public class GameEntityCommonView : GameComponentView
    {
        public TMP_InputField nameInput;

        // Start is called before the first frame update
        public override void Start()
        {
            nameInput.onValueChanged.AddListener((value) =>
            {
                if (target)
                {
                    target.name = value;
                }
            });
        }

        // Update is called once per frame
        //void Update()
        //{

        //}

        public override void OnNewTarget(GameObject target)
        {
            nameInput.text = target.name;
        }

        public override bool CheckTargetValid()
        {
            return target != null;
        }
    }
}
