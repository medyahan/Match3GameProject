using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "GameData", menuName = "Data/Match3Game/GameData")]
    public class GameData : ScriptableObject
    {
        public int BoardWidth;
        public int BoardHeight;

        [Space] public List<DropData> _dropDataList;

        public DropData GetRandomDropData()
        {
            return _dropDataList[Random.Range(0, _dropDataList.Count)];
        }
    }
    
    
}
