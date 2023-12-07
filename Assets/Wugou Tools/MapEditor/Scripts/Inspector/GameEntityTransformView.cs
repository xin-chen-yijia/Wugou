using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.MapEditor{
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

            positionView.AddUpdateEvent(() =>
            {
                target.transform.position = positionView.vec3Val;
            });

            rotationView.AddUpdateEvent(() =>
            {
                target.transform.eulerAngles = rotationView.vec3Val;
            });

            scaleView.AddUpdateEvent(() =>
            {
                target.transform.localScale = scaleView.vec3Val;
            });
            scaleView.SetValue(Vector3.one);
            scaleView.SetInteractable(false);
        }

        public void Update()
        {
            if (target && target.transform.hasChanged)
            {
                positionView.SetValue(target.transform.position);
                rotationView.SetValue(target.transform.eulerAngles);
                //scaleView.SetValue(target.transform.localScale);
            }
        }
    }
}
