using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou.Editor.UI;
using Wugou.UI;

namespace Wugou
{
    /// <summary>
    /// ´¥·¢Æ÷
    /// </summary>
    [CustomGameComponentView(typeof(TriggerBoxView))]
    [EntitySerializable]
    public class TriggerBox : GameComponent, IPointerClickSolver
    {
        public enum TriggerType
        {
            kCollision = 0, // Åö×²
            kClick  // µã»÷
        }

        [EntitySerializeField]
        public TriggerType triggerType = TriggerType.kClick;

        public enum ResponseType
        {
            kPopup= 0,
            kMethod
        }

        [EntitySerializeField]
        public ResponseType responseType = ResponseType.kPopup;

        [EntitySerializeField]
        public string targetName;

        [EntitySerializeField]
        public string targetMethod;

        [EntitySerializeField]
        public string targetParameter;

        [EntitySerializeField]
        public string message;

        private void OnTriggerEnter(Collider other)
        {
            if (triggerType == TriggerType.kCollision)
            {
                TriggerInternal(other.gameObject);
            }
        }

        public void OnPointerClick(PointerEventContext context)
        {
            if (triggerType == TriggerType.kClick)
            {
                TriggerInternal(context.source);
            }
        }

        private void TriggerInternal(GameObject player)
        {
            if (responseType == ResponseType.kPopup)
            {
                ShowTipPage(message);
            }
            else
            {
                HandleMethodTrigger(player);
            }

        }

        protected virtual void ShowTipPage(string message)
        {
            DaemonUI.makeSurePage.Show(message);
        }

        protected virtual void HandleMethodTrigger(GameObject player)
        {
            var entity = GameWorld.Find(targetName);
            if (entity != null)
            {
                entity.gameObject.SendMessage(targetName, targetParameter, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
//#if UNITY_2022_3_OR_NEWER
//                var player = GameObject.FindAnyObjectByType<GamePlayer>();

//#else
//                    var player = GameObject.FindObjectOfType<GamePlayer>();
//#endif
                player.gameObject.SendMessage(targetMethod, targetParameter, SendMessageOptions.DontRequireReceiver);
            }
        }

        // Start is called before the first frame update
        //void Start()
        //{
        //}

        //// Update is called once per frame
        //void Update()
        //{

        //}
    }
}
