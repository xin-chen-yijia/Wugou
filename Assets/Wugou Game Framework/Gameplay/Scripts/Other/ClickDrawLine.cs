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
        private ClickDrawLine() { }

        public LineRenderer lineRender{ get; private set; }

        public int selectableMask { get; set; } = Physics.AllLayers & ~(Physics.IgnoreRaycastLayer);

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
            var tmpLine = CreateLine(lineMaterial, width);
            lineRender = tmpLine;

            tmpLine.positionCount = 1;
            Vector3 biasOffset = new Vector3(0,bias, 0);

            float lastClickTime_ = -1;
            isEnd_ = false;
            while (!isEnd_ && tmpLine)
            {
                RaycastHit hit;
                if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hit, 10000, selectableMask))
                {
                    var curP = hit.point + biasOffset;

                    // 避免线变细
                    if(tmpLine.positionCount == 1 || (curP - tmpLine.GetPosition(tmpLine.positionCount - 2)).sqrMagnitude > 0.05f)
                    {
                        tmpLine.SetPosition(tmpLine.positionCount - 1, curP);
                    }
                }

                if (Input.GetMouseButtonDown(0) && !Utils.IsPointerOnUI())
                {
                    if(Time.realtimeSinceStartup - lastClickTime_ < Utils.doubleClickMaxInterval)
                    {
                        // 双击，两个点离得近
                        if (tmpLine.positionCount > 1 && (tmpLine.GetPosition(tmpLine.positionCount - 1) - tmpLine.GetPosition(tmpLine.positionCount - 2)).magnitude <= 0.05f)
                        {
                            tmpLine.positionCount = tmpLine.positionCount - 1; 
                        }

                        End();
                    }
                    else
                    {

                        tmpLine.positionCount = tmpLine.positionCount + 1;
                        tmpLine.SetPosition(tmpLine.positionCount - 1, lineRender.GetPosition(tmpLine.positionCount - 2));

                        lastClickTime_ = Time.realtimeSinceStartup;
                    }

                }

                yield return null;
            }
        }

        public void End()
        {
            isEnd_ = true;
            lineRender.positionCount = lineRender.positionCount - 1;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 取消当前的画线
        /// </summary>
        public void Cancel()
        {
            isEnd_ = true;
            if (lineRender)
            {
                GameObject.Destroy(lineRender.gameObject);
                lineRender = null;
            }
        }

        public void Clear()
        {
            if (lineRender)
            {
                GameObject.Destroy(lineRender.gameObject);
                lineRender = null;
            }
        }

        public static ClickDrawLine Create()
        {
            return new ClickDrawLine();
        }

        public static LineRenderer CreateLine(Material lineMaterial, float width)
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
            if(lineRender != null)
            {
                Vector3[] positions = new Vector3[lineRender.positionCount];
                lineRender.GetPositions(positions);

                return positions;
            }

            return new Vector3[0];
        }
    }
}

