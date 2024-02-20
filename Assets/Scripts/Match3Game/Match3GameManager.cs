using Core;
using Data;
using Match3Game.GameBoard;
using UnityEngine;

namespace Match3Game
{
    public class Match3GameManager : BaseMonoBehaviour
    {
        [SerializeField] private GameData _gameData;
        [SerializeField] private GameBoardManager _gameBoardManager;
    
        public override void Initialize(params object[] list)
        {
            base.Initialize(list);
        
            _gameBoardManager.Initialize(_gameData);
        }

        public override void RegisterEvents()
        {
            
        }

        public override void UnregisterEvents()
        {
            
        }
    }
}
