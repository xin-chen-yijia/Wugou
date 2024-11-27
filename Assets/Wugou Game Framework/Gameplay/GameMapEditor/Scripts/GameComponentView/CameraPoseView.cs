using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wugou.Editor.UI
{
    public class CameraPoseView : GameComponentView
    {
        public Button locateButton;

        private Camera _previewCamera;
        public Camera previewCamera
        {
            get
            {
                if (!_previewCamera)
                {
                    var go = new GameObject("Small View Camera", new System.Type[] { typeof(Camera) });

                    _previewCamera = go.GetComponent<Camera>();
                    _previewCamera.CopyFrom(GameMapEditor.instance.editorCamera);

                    previewTexture_ = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32);
                    _previewCamera.targetTexture = previewTexture_;

                    _previewCamera.enabled = false;

                    GameMapEditor.instance.uiRootWindow.GetChildWindow<SmallCameraViewPage>().SetTexture(previewTexture_);
                }

                return _previewCamera;
            }
        }
        private RenderTexture previewTexture_;


        public override bool CheckTargetValid()
        {
            return componentGameObject != null && componentGameObject.GetComponent<GameEntity>();
        }

        public override void Start()
        {
            base.Start();

            locateButton.onClick.AddListener(() =>
            {
                var cam = GameMapEditor.instance.editorCamera;

                var entity = componentGameObject.GetComponent<GameEntity>();
                entity.position = cam.transform.position;
                entity.rotation = cam.transform.rotation;

                previewCamera.transform.position = cam.transform.position;
                previewCamera.transform.rotation = cam.transform.rotation;
            });
        }

        public override void OnNewTarget(GameObject target)
        {
            base.OnNewTarget(target);

            previewCamera.transform.position = target.transform.position;
            previewCamera.transform.rotation = target.transform.rotation;
        }

        public override void Show()
        {
            base.Show();

            previewCamera.enabled = true;
            GameMapEditor.instance.uiRootWindow.GetChildWindow<SmallCameraViewPage>().Show();
        }

        public override void Hide()
        {
            base.Hide();

            previewCamera.enabled = false;
            GameMapEditor.instance.uiRootWindow.GetChildWindow<SmallCameraViewPage>().Hide();
        }

    }
}

