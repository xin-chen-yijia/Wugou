using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Wugou
{
    public class TransformEasing
    {
        public class TweenBlock
        {
            public bool isRunning = false;
        }

        private static IEnumerator TweenInternal(TweenBlock block, System.Action<float> applyAction, float speed)
        {
            float p = 0.0f;
            while (block.isRunning && p < 1.0f)
            {
                applyAction.Invoke(p);
                p += Time.deltaTime * speed;

                yield return null;
            }

            applyAction.Invoke(1.0f);
            block.isRunning = false;
        }

        /// <summary>
        /// 用于过渡动画
        /// </summary>
        /// <param name="applyAction"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static TweenBlock Tween(System.Action<Vector3> applyAction, Vector3 start, Vector3 end, float speed=1.0f)
        {
            var block = new TweenBlock() { isRunning = true };
            CoroutineLauncher.active.StartCoroutine(TweenInternal(block, (p) =>
            {
                applyAction.Invoke(Vector3.Lerp(start, end, p));
            }, speed));

            return block;
        }

        public static TweenBlock Tween(System.Action<Vector3> applyAction, List<Vector3> array, float speed = 1.0f)
        {
            if (array.Count == 0)
            {
                return new TweenBlock();
            }
            if (array.Count == 1)
            {
                applyAction.Invoke(array[0]);
                return new TweenBlock();
            }

            var block = new TweenBlock() { isRunning = true };
            CoroutineLauncher.active.StartCoroutine(TweenPath(block, applyAction, array, speed));

            return block;
        }

        private static IEnumerator TweenPath(TweenBlock block, System.Action<Vector3> applyAction, List<Vector3> array, float speed)
        {
            float p = 0.0f;
            int index = 0;
            Vector3 start = array[index];
            Vector3 end = array[index+1];
            while (block.isRunning && p < 1.0f)
            {
                applyAction.Invoke(Vector3.Lerp(start, end, p));
                p += Time.deltaTime * speed;
                if(p >= 1.0f && index < array.Count-2)
                {
                    ++index;
                    start = array[index];
                    end = array[(index+1)];
                    p = 0;
                }

                yield return null;
            }

            applyAction.Invoke(end);
            block.isRunning = false;
        }

        /// <summary>
        /// 用于过渡动画
        /// </summary>
        /// <param name="applyAction"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static TweenBlock Tween(System.Action<Quaternion> applyAction, Quaternion start, Quaternion end, float speed = 1.0f)
        {
            var block = new TweenBlock() { isRunning = true };
            CoroutineLauncher.active.StartCoroutine(TweenInternal(block, (p) =>
            {
                applyAction.Invoke(Quaternion.Slerp(start, end, p));
            }, speed));

            return block;
        }

        /// <summary>
        /// 用于动画
        /// </summary>
        /// <param name="applyAction"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static TweenBlock Tween(System.Action<Vector3, Quaternion> applyAction, Transform start, Transform end, float speed = 1.0f)
        {
            var block = new TweenBlock() { isRunning = true };
            CoroutineLauncher.active.StartCoroutine(TweenInternal(block, (p) =>
            {
                var pos = Vector3.Lerp(start.position, end.position, p);
                var rotation = Quaternion.Lerp(start.rotation, end.rotation, p);
                applyAction.Invoke(pos, rotation);
            }, speed));

            return block;
        }
    }
}
