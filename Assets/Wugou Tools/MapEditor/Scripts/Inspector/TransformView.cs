using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.MapEditor{
    public class TransformView : GameComponentView
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

            positionView.onValueChanged.AddListener((val) =>
            {
                target.transform.position = val;
                print(target.name +  " pos:" + val);
            });

            rotationView.onValueChanged.AddListener((val) =>
            {
                target.transform.eulerAngles = val;
            });

            scaleView.onValueChanged.AddListener((val) =>
            {
                target.transform.localScale = val;
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
