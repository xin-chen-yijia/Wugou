using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Wugou.UI;

namespace Wugou.Editor
{
    /// <summary>
    /// 调色板
    /// </summary>
    public class Palette : UIBaseWindow, IPointerClickHandler, IDragHandler
    {
        public Image curColorImage;     // 显示当前颜色
        public RawImage colorTape; // 彩带
        public RawImage colorBoard;    //中间的颜色板
        public Image circleImage;   // 颜色板上的光标

        public Slider colorTapeSlider;
        public Slider alphaSlider;

        public TMPro.TMP_InputField colorInput;
        public TMPro.TMP_InputField alphaInput;

        public Button closeButton;

        private Texture2D colorTapeTexture_;
        private Texture2D boardTexture_;

        public const int kBoardWidth = 256;
        public const int kBoardHeight = 256;
        private Color[,] arrayColor = new Color[kBoardWidth, kBoardHeight];

        public UnityEvent<Color> onColorChanged = new UnityEvent<Color>();

        private RectTransform boardTrans_;
        private RectTransform circleTrans_;

        /// <summary>
        /// 当前颜色
        /// </summary>
        public Color curColor { get; private set; }

        #region 颜色彩带

        /// <summary>
        /// 创建颜色彩带纹理
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private Texture2D CreateColorTapeTexture(int width, int height)
        {
            var tex2d = new Texture2D(width, height, TextureFormat.RGB24, true);
            List<Color> listColor = new List<Color>();
            for (int x = 0; x <= width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    var pixColor = Color.HSVToRGB((float)x / width, 1, 1);
                    tex2d.SetPixel(x, y, pixColor);
                }
            }

            tex2d.wrapMode = TextureWrapMode.Clamp;
            tex2d.Apply();
            return tex2d;
        }

        /// <summary>
        /// 获取颜色
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Color GetColorBySliderValue(float value)
        {
            float clampValue = Mathf.Clamp(value, 0.001f, 0.999f);
            Color getColor = colorTapeTexture_.GetPixel((int)(clampValue * (colorTapeTexture_.width - 1)), 0);
            return getColor;
        }

        #endregion

        #region 中间颜色板

        public void SetBoardColor(Color endColor)
        {
            boardTexture_.SetPixels(GetColors(endColor));
            boardTexture_.Apply();
        }

        private Color[] GetColors(Color endColor)
        {
            Color value = (endColor - Color.white) / (kBoardWidth - 1);
            for (int i = 0; i < kBoardWidth; i++)
            {
                arrayColor[i, kBoardHeight - 1] = Color.white + value * i;
            }

            //
            for (int i = 0; i < kBoardWidth; i++)
            {
                value = (arrayColor[i, kBoardHeight - 1] - Color.black) / (kBoardHeight - 1);
                for (int j = 0; j < kBoardHeight; j++)
                {
                    arrayColor[i, j] = Color.black + value * j;
                }
            }

            List<Color> listColor = new List<Color>();
            for (int i = 0; i < kBoardHeight; i++)
            {
                for (int j = 0; j < kBoardHeight; ++j)
                {
                    listColor.Add(arrayColor[j, i]);
                }
            }

            return listColor.ToArray();
        }

        public Color GetBoardColorByPosition(Vector2 pos)
        {
            int x = (int)((pos.x / boardTrans_.sizeDelta.x) * boardTexture_.width);
            x = (x >= boardTexture_.width) ? boardTexture_.width - 1 : x;
            int y = (int)((pos.y / boardTrans_.sizeDelta.y) * boardTexture_.height);
            y = (y >= boardTexture_.height) ? boardTexture_.height - 1 : y;

            return boardTexture_.GetPixel(x, y);
        }

        public Vector2 GetClampPosition(Vector2 touchPos)
        {
            return new Vector2(Mathf.Clamp(touchPos.x, 0.001f, boardTrans_.sizeDelta.x), Mathf.Clamp(touchPos.y, 0.001f, boardTrans_.sizeDelta.y));
        }

        #endregion

        public override void Awake()
        {
            base.Awake();
            // color tape
            colorTapeTexture_ = CreateColorTapeTexture(270, 8);
            colorTape.texture = colorTapeTexture_;

            // board
            boardTexture_ = new Texture2D(256, 256, TextureFormat.RGB24, true);
            boardTexture_.wrapMode = TextureWrapMode.Clamp;
            colorBoard.texture = boardTexture_;

            boardTrans_ = colorBoard.GetComponent<RectTransform>();
            circleTrans_ = circleImage.GetComponent<RectTransform>();

            // show white first
            SetCurColor(Color.white);
        }

        // Start is called before the first frame update
        void Start()
        {
            // 
            colorTapeSlider.onValueChanged.AddListener((val) =>
            {
                var c = GetColorBySliderValue(val);
                SetBoardColor(c);

                var color = GetBoardColorByPosition(circleTrans_.anchoredPosition);
                color.a = curColor.a;
                SetCurColorInternal(color, true);

                colorInput.SetTextWithoutNotify($"{ColorUtility.ToHtmlStringRGB(c).Substring(1)}");
            });

            // alpha
            alphaSlider.onValueChanged.AddListener((value) =>
            {
                SetCurColorInternal(new Color(curColor.r, curColor.g, curColor.b, value), true);

                alphaInput.SetTextWithoutNotify($"{(int)(value * 255)}");
            });

            // input 
            colorInput.onValueChanged.AddListener((val) =>
            {
                if (ColorUtility.TryParseHtmlString($"#{val}", out Color c))
                {
                    SetBoardColor(c);
                    //
                    circleTrans_.anchoredPosition = boardTrans_.sizeDelta;

                    float h, s, v;
                    Color.RGBToHSV(c, out h, out s, out v);
                    colorTapeSlider.SetValueWithoutNotify(h);

                    SetCurColorInternal(new Color(c.r, c.g, c.b, curColor.a), true);
                }
            });

            alphaInput.onValueChanged.AddListener((val) =>
            {
                if(int.TryParse(val, out int alpha))
                {
                    alpha = alpha > 255 ? 255 : alpha;
                    alpha = alpha < 0 ? 0 : alpha;
                    alphaInput.SetTextWithoutNotify(alpha.ToString());

                    var a = alpha / 255.0f;
                    alphaSlider.SetValueWithoutNotify(a);
                    SetCurColorInternal(new Color(curColor.r, curColor.g, curColor.b, a), true);
                }
            });

            closeButton.onClick.AddListener(() =>
            {
                Hide();
            });
        }

        /// <summary>
        /// 设置当前颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetCurColor(Color color)
        {
            // 不发送事件，这个接口一般用于外部调用
            SetCurColorInternal(color, false);

            // change color board, color slider value, alpha
            //
            SetBoardColor(color);

            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            colorTapeSlider.SetValueWithoutNotify(h);
            alphaSlider.SetValueWithoutNotify(color.a);

            //
            circleTrans_.anchoredPosition = boardTrans_.sizeDelta;

            colorInput.SetTextWithoutNotify($"{ColorUtility.ToHtmlStringRGB(curColor)}");
            alphaInput.SetTextWithoutNotify($"{(int)(curColor.a * 255)}");
        }

        private void SetCurColorInternal(Color color, bool sendCallback = false)
        {
            curColor = color;
            curColorImage.color = color;

            if (sendCallback)
            {
                onColorChanged.Invoke(color);
            }
        }

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public void OnPointerClick(PointerEventData eventData)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(boardTrans_, eventData.position))
            {
                Vector3 pos;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(boardTrans_, eventData.position, eventData.pressEventCamera, out pos))
                {
                    circleTrans_.position = pos;
                }

                circleTrans_.anchoredPosition = GetClampPosition(circleTrans_.anchoredPosition);

                var color = GetBoardColorByPosition(circleTrans_.anchoredPosition);
                color.a = curColor.a;
                SetCurColorInternal(color, true);

            }

        }

        public void OnDrag(PointerEventData eventData)
        {
            //if (RectTransformUtility.RectangleContainsScreenPoint(boardTrans_, eventData.position))
            {
                Vector3 pos;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(boardTrans_, eventData.position, eventData.pressEventCamera, out pos))
                {
                    circleTrans_.position = pos;
                }

                circleTrans_.anchoredPosition = GetClampPosition(circleTrans_.anchoredPosition);

                var color = GetBoardColorByPosition(circleTrans_.anchoredPosition);
                color.a = curColor.a;
                SetCurColorInternal(color, true);
            }

        }
    }
}
