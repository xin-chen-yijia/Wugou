using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Wugou
{
    /// <summary>
    /// 用于画线
    /// </summary>
    public class ClickDrawLine
    {
        private LineRenderer currentLineRender_;

        /// <summary>
        /// 画线结束后调用
        /// </summary>
        public UnityEvent onComplete = new UnityEvent();

        /// <summary>
        /// 开始画线
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="lineMaterial"></param>
        /// <param name="bias"></param>
        public void Start(Camera camera, Material lineMaterial, float width = 1.0f, float bias = 0.01f)
        {
            CoroutineLauncher.active.StartCoroutine(DrawlineInternal(camera, lineMaterial, width, bias));
        }

        private bool isEnd_ = false;

        IEnumerator DrawlineInternal(Camera camera, Material lineMaterial, float width, float bias)
        {
            var lineRenderer = CreateLineRender(lineMaterial, width);

            currentLineRender_ = lineRenderer;

            int pointCount = 1;
            lineRenderer.positionCount = pointCount;
            Vector3 biasOffset = new Vector3(0,bias, 0);

            while(!isEnd_)
            {
                RaycastHit hit;
                if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hit))
                {
                    lineRenderer.SetPosition(pointCount - 1, hit.point + biasOffset);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    ++pointCount;
                    lineRenderer.positionCount = pointCount;
                }

                if (Input.GetKeyDown(KeyCode.Q))
                {
                    End();
                }

                if((Input.GetKey(KeyCode.LeftAlt) ||  Input.GetKeyUp(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.Q))
                {
                    Cancel();
                }

                yield return null;
            }
        }

        public void End()
        {
            isEnd_ = true;
            currentLineRender_.positionCount = currentLineRender_.positionCount - 1;

            onComplete?.Invoke();
        }

        /// <summary>
        /// 取消当前的画线
        /// </summary>
        public void Cancel()
        {
            End();
            if (currentLineRender_)
            {
                GameObject.Destroy(currentLineRender_);
            }
        }

        public static LineRenderer CreateLineRender(Material lineMaterial, float width)
        {
            // create linerender
            GameObject go = new GameObject("LineRender", typeof(LineRenderer));
            go.transform.localEulerAngles = new Vector3(90, 0, 0);
            LineRenderer lineRenderer = go.GetComponent<LineRenderer>();
            lineRenderer.material = lineMaterial;
            lineRenderer.alignment = LineAlignment.TransformZ;
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.numCornerVertices = 6;
            lineRenderer.widthMultiplier = width;

            return lineRenderer;
        }

        public Vector3[] GetPositions()
        {
            if(currentLineRender_ != null)
            {
                Vector3[] positions = new Vector3[currentLineRender_.positionCount];
                currentLineRender_.GetPositions(positions);

                return positions;
            }

            return new Vector3[0];
        }
    }
}

