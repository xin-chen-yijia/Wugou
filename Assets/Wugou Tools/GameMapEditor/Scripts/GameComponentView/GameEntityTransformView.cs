using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.Editor{
    public class GameEntityTransformView : GameComponentView
    {
        public Vector3PropertyView positionView;
        public Vector3PropertyView rotationView;
        public Vector3PropertyView scaleView;

        public override bool CheckTargetValid()
        {
            return target != null;
        }

        public override void Start()
        {
            base.Start();

            positionView.AddUpdateEvent((val) =>
            {
                target.transform.position = val;
            });

            rotationView.AddUpdateEvent((val) =>
            {
                target.transform.eulerAngles = val;
            });

            scaleView.AddUpdateEvent((val) =>
            {
                target.transform.localScale = val;
            });

            // scale not editable
            //scaleView.SetValue(Vector3.one);
            //scaleView.SetInteractable(false);
        }

        public void Update()
        {
            if (target && target.transform.hasChanged)
            {
                positionView.SetValue(target.transform.position);
                rotationView.SetValue(target.transform.eulerAngles);
                scaleView.SetValue(target.transform.localScale);
            }
        }
    }
}
