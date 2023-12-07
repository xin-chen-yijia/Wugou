using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Wugou;
using Wugou.UI;
using Wugou.Multiplayer;
using UnityEngine.UI;

namespace Wugou.Examples.UI
{
    public class SimpleLobbyPage : UIBaseWindow
    {
        public Button QuitButton;
        // Start is called before the first frame update
        void Start()
        {
            QuitButton.onClick.AddListener(Quit);

        }

        //// Update is called once per frame
        //void Update()
        //{

        //}

        public void Quit()
        {
            MultiplayerGameManager.instance.Quit();
        }

    }

}
