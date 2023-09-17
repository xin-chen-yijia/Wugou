using Newtonsoft.Json.Linq;
using Wugou.MapEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Wugou
{
    [CustomPropertyView("PathWalker")]
    public class PathWalker : GameComponent
    {
        public struct PathPoint
        {
            public Vector3 position;
            public Vector3 eulerAngles;
            //public Vector3 scale;
        }

        //路径点
        public List<PathPoint> points { get; private set; } = new List<PathPoint>();

        public float speed = 10;    

        public UnityEvent OnWalkStop = new UnityEvent();

        private PathPoint cachedTransform;

        /// <summary>
        /// 状态标记，true表示正在行走
        /// </summary>
        public bool isRunning { get; private set; }

        public override void BeginPlay()
        {
            StartWalk();
        }


        private int index_ = 0;
        public void StartWalk()
        {
            if (points.Count == 0)
            {
                return;
            }

            StartCoroutine(Walk());
        }

        public void Pause()
        {
            isRunning = false;
        }

        public void Restart()
        {
            isRunning = true;
        }

        public void Stop()
        {
            isRunning = false;
            StopAllCoroutines();
            OnWalkStop.Invoke();
        }

        public void PushState()
        {
            cachedTransform = new PathPoint
            {
                position = transform.position,
                eulerAngles = transform.eulerAngles,
            };
        }

        public void PopState()
        {
            isRunning = false;
            transform.position = cachedTransform.position;
            transform.eulerAngles = cachedTransform.eulerAngles;
        }

        IEnumerator Walk()
        {
            transform.position = points[0].position;
            if (points.Count > 1)
            {
                transform.forward = (points[1].position - points[0].position).normalized;

                index_ = 0;
                isRunning = true;
                while (index_ + 1 < points.Count)
                {
                    if (isRunning)
                    {
                        transform.Translate(new Vector3(0, 0, speed * Time.deltaTime), Space.Self);

                        var curDir = (points[index_ + 1].position - transform.position);
                        if (Vector3.Dot(transform.forward, curDir) < 0) // 
                        {
                            index_++;
                            if (index_ + 1 < points.Count)
                            {
                                transform.forward = (points[index_ + 1].position - points[index_].position).normalized;
                            }
                        }
                    }

                    yield return null;
                }
            }

            isRunning = false;
            OnWalkStop.Invoke();
        }

        //public override string Serialize()
        //{
        //    JObject jo = new JObject();
        //    jo.Add("points", JArray.FromObject(points, new Newtonsoft.Json.JsonSerializer { Converters = {new Vector3Converter()}}));
        //    jo.Add("speed",speed);

        //    return jo.ToString();
        //}

        //public override void Deserialize(string value)
        //{
        //    JObject jo = JObject.Parse(value);
        //    points = ((JArray)jo["points"]).ToObject<List<PathPoint>>(new Newtonsoft.Json.JsonSerializer { Converters = { new Vector3Converter() } });
        //    speed = jo["speed"].ToObject<float>();
        //}
    }
}
