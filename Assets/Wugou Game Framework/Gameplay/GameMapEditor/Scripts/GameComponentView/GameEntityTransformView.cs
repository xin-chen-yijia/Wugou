using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou.Editor.UI
{
    public class GameEntityTransformView : GameComponentView
    {
        public Vector3PropertyView positionView;
        public Vector3PropertyView rotationView;
        public Vector3PropertyView scaleView;

        public override bool CheckTargetValid()
        {
            return componentGameObject != null;
        }

        public override void Start()
        {
            base.Start();

            positionView.SetValueChangedCallback((val) =>
            {
                componentGameObject.transform.position = (Vector3)val;
            });

            rotationView.SetValueChangedCallback((val) =>
            {
                componentGameObject.transform.eulerAngles = (Vector3)val;
            });

            scaleView.SetValueChangedCallback((val) =>
            {
                componentGameObject.transform.localScale = (Vector3)val;
            });

            // scale not editable
            //scaleView.SetValue(Vector3.one);
            //scaleView.SetInteractable(false);
        }

        public void Update()
        {
            if (componentGameObject && componentGameObject.transform.hasChanged)
            {
                positionView.SetValue(componentGameObject.transform.position);
                rotationView.SetValue(componentGameObject.transform.eulerAngles);
                scaleView.SetValue(componentGameObject.transform.localScale);
            }
        }
    }
}
