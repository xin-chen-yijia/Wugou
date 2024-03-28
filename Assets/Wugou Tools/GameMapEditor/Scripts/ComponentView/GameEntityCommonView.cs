using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wugou.Editor
{
    public class GameEntityCommonView : GameComponentView
    {
        public TMP_InputField nameInput;
        public Toggle enableToggle;

        // Start is called before the first frame update
        public override void Start()
        {
            nameInput.onValueChanged.AddListener((value) =>
            {
                if (target)
                {
                    target.GetComponent<GameEntity>().name = value;
                }
            });

            enableToggle.onValueChanged.AddListener((value) =>
            {
                target.SetActive(value);
            });
        }

        // Update is called once per frame
        void Update()
        {
            if(!nameInput.isFocused)
            {
                nameInput.SetTextWithoutNotify(target.GetComponent<GameEntity>().name);
            }
        }

    public override void OnNewTarget(GameObject target)
        {
            nameInput.SetTextWithoutNotify(target.GetComponent<GameEntity>().name);
            enableToggle.SetIsOnWithoutNotify(target.activeSelf);
        }

        public override bool CheckTargetValid()
        {
            return target != null;
        }
    }
}
