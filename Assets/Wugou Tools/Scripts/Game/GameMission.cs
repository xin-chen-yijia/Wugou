using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Wugou.UI;

namespace Wugou
{
    public class GameMission : MonoBehaviour
    {
        public string missionName;
        public string description;

        public virtual async Task<bool> Init()
        {
            return await Task.FromResult(true);
        }
    }
}
