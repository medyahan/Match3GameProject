using System.Collections;
using System.Collections.Generic;
using Core;
using Data;
using DG.Tweening;
using Enum;
using UnityEngine;

namespace Match3Game.GameBoard
{
    public class GameBoardManager : Singleton<GameBoardManager>
    {
        #region Variable Fields

        public GameStateType CurrentGameState { get; set; }
        
        [Header("GRID VALUES")] 
        [SerializeField] private Transform _gridBackgroundTransform;

        private int _gridWidth;
        private int _gridHeight;

        [Header("TILE")] 
        [SerializeField] private Tile _tilePrefab;

        [Header("DROP")] 
        [SerializeField] private Drop.Drop _dropPrefab;
        [SerializeField] private int _dropPoolCount;
        [SerializeField] private float _dropFallingDuration;

        [Header("SPAWN VALUES")] 
        [SerializeField] private float _spawnOffset;
        [SerializeField] private List<bool> _spawnColumnList = new List<bool>();

        private ObjectPool<Drop.Drop> _dropPool;
        private Drop.Drop[,] _dropArray;
        
        public Drop.Drop[,] DropArray => _dropArray;
        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;

        private GameData _gameData;

        #endregion // Variable Fields
        
        public void Initialize(params object[] list)
        {
            _gameData = (GameData) list[0];

            _gridWidth = _gameData.BoardWidth;
            _gridHeight = _gameData.BoardHeight;

            _dropArray = new Drop.Drop[_gridWidth, _gridHeight];

            _dropPool = new ObjectPool<Drop.Drop>(transform, _dropPrefab, _dropPoolCount);

            AddSpawnColumns();
            CreateGrid();
        }

        /// <summary>
        /// Ensures that there are enough spawn columns based on the specified grid width.
        /// </summary>
        private void AddSpawnColumns()
        {
            while (_spawnColumnList.Count < _gridWidth)
            {
                // Add a spawn column if there are not enough columns.
                _spawnColumnList.Add(true);
            }
        }

        #region CREATE METHODS

        /// <summary>
        /// Creates a grid with tiles and drops based on the specified width, height, and spawn offset.
        /// </summary>
        private void CreateGrid()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    Vector2 position = new Vector2(x, y + _spawnOffset);

                    CreateTile(new Vector2(x, y));
                    CreateDrop(position, x, y);
                }
            }
            
            // Adjust the scale of the grid background to match the grid dimensions.
            _gridBackgroundTransform.localScale = new Vector3(_gridWidth, _gridHeight);
        }

        /// <summary>
        /// Creates a tile at the specified position.
        /// </summary>
        /// <param name="pos">Position of the tile.</param>
        private void CreateTile(Vector2 pos)
        {
            Tile tile = Instantiate(_tilePrefab, pos, Quaternion.identity, transform);
            tile.gameObject.name = "(" + pos.x + ", " + pos.y + ")";

            tile.Initialize();
        }

        /// <summary>
        /// Creates a drop at the specified position with coordinates (column, row).
        /// </summary>
        /// <param name="pos">Position of the drop.</param>
        /// <param name="column">Column index of the drop.</param>
        /// <param name="row">Row index of the drop.</param>
        private void CreateDrop(Vector2 pos, int column, int row)
        {
            if( _spawnColumnList[column] == false) return;
                
            Drop.Drop drop = _dropPool.GetObject(pos, Quaternion.identity);

            drop.transform.parent = transform;
            drop.transform.position = pos;
            drop.gameObject.name = "(" + column + ", " + row + ")";

            int maxIteration = 0;
            DropData dropData = _gameData.GetRandomDropData();
            while (CheckForMatches(column, row, dropData) && maxIteration < 100)
            {
                dropData = _gameData.GetRandomDropData();
                maxIteration++;
            }

            drop.Initialize(dropData);
            drop.Row = row;
            drop.Column = column;  
            drop.MoveTo(column, row, 1f);
            _dropArray[column, row] = drop;
        }

        #endregion

        /// <summary>
        /// Checks for potential matches of the given drop in its surrounding positions.
        /// </summary>
        /// <param name="column">Column index of the drop.</param>
        /// <param name="row">Row index of the drop.</param>
        /// <param name="dropData">Drop data to check for matches.</param>
        /// <returns>True if there are potential matches, false otherwise.</returns>
        private bool CheckForMatches(int column, int row, DropData dropData)
        {
            if (column > 1 && row > 1)
            {
                if (_dropArray[column - 1, row]?.Data == dropData && _dropArray[column - 2, row]?.Data == dropData)
                    return true;

                if (_dropArray[column, row - 1]?.Data == dropData && _dropArray[column, row - 2]?.Data == dropData)
                    return true;
            }
            else if (column <= 1 || row <= 1)
            {
                if (row > 1)
                {
                    if (_dropArray[column, row - 1]?.Data == dropData && _dropArray[column, row - 2]?.Data == dropData)
                        return true;
                }

                if (column > 1)
                {
                    if (_dropArray[column - 1, row]?.Data == dropData && _dropArray[column - 2, row]?.Data == dropData)
                        return true;
                }
            }

            return false;
        }

        #region DESTROY DROP TRANSACTIONS
        
        /// <summary>
        /// Destroys a matched drop at the specified grid position.
        /// </summary>
        /// <param name="column">Column index of the drop.</param>
        /// <param name="row">Row index of the drop.</param>
        private void DestroyMatchedDropAt(int column, int row)
        {
            Drop.Drop drop = _dropArray[column, row];
            if (drop.IsMatched)
            {
                drop.End();
                _dropArray[column, row] = null;
                StartCoroutine(ReturnDropToPoolCoroutine(drop));
            }
        }

        private IEnumerator ReturnDropToPoolCoroutine(Drop.Drop drop)
        {
            yield return new WaitForSeconds(.2f);
            _dropPool.ReturnObject(drop);
        }
        
        /// <summary>
        /// Destroys all matched drops on the grid.
        /// </summary>
        public void DestroyMatchedDrops()
        {
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (_dropArray[x, y] != null)
                    {
                        DestroyMatchedDropAt(x, y);
                    }
                }
            }
            // Shift down empty rows after destroying matched drops.
            StartCoroutine(ShiftDownEmptyRowsCoroutine());
        }
        
        #endregion

        /// <summary>
        /// Shifts down drops in empty rows and refills the board.
        /// </summary>
        private IEnumerator ShiftDownEmptyRowsCoroutine()
        {
            int nullCount = 0;
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int y = 0; y < _gridHeight; y++)
                {
                    if (_dropArray[x, y] == null)
                    {
                        nullCount++;
                    }
                    else if (nullCount > 0)
                    {
                        int newRow = _dropArray[x, y].Row;
                        newRow -= nullCount;
                        
                        _dropArray[x, y].MoveTo(x, newRow, _dropFallingDuration);
                        _dropArray[x, y].Row = newRow;
                        _dropArray[x, newRow] = _dropArray[x, y];
                        _dropArray[x, y] = null;
                    }
                }

                nullCount = 0;
            }
            yield return new WaitForSeconds(_dropFallingDuration);
            
            // Refill the board after shifting down empty rows.
            StartCoroutine(RefillBoardCoroutine());
        }

        #region REFILL TRANSACTIONS
        
        /// <summary>
        /// Refills the board with new drops and checks for matches.
        /// </summary>
        private IEnumerator RefillBoardCoroutine(){
            RefillBoard();
            yield return new WaitForSeconds(_dropFallingDuration);

            // Check for matches and destroy matched drops until no matches are found.
            while(HasMatchesOnBoard()){
                yield return new WaitForSeconds(_dropFallingDuration);
                DestroyMatchedDrops();
            }
            // Continue the game after the refill and match destruction process.
            yield return new WaitForSeconds(_dropFallingDuration);
            CurrentGameState = GameStateType.Continue;
        }
        
        /// <summary>
        /// Refills the empty spaces on the board with new drops.
        /// </summary>
        private void RefillBoard(){
            for (int x = 0; x < _gridWidth; x ++)
            {
                // Skip columns where drops should not spawn.
                if( _spawnColumnList[x] == false) continue;
                
                for (int y = 0; y < _gridHeight; y ++)
                {
                    // Spawn a new drop in empty spaces.
                    if(_dropArray[x, y] == null)
                    {
                        Vector2 pos = new Vector2(x, _spawnOffset);
                        Drop.Drop drop = _dropPool.GetObject(pos, Quaternion.identity);

                        drop.transform.parent = transform;
                        drop.transform.position = pos;
                        drop.gameObject.name = "(" + pos.x + ", " + pos.y + ")";

                        drop.Initialize(_gameData.GetRandomDropData(), this);
                        _dropArray[x, y] = drop;
                        drop.Row = y;
                        drop.Column = x;
                        drop.MoveTo(x,  y, _dropFallingDuration);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if there are any matches on the game board.
        /// </summary>
        /// <returns><c>true</c> if there are matches; otherwise, <c>false</c>.</returns>
        private bool HasMatchesOnBoard(){
            for (int i = 0; i < _gridWidth; i ++){
                for (int j = 0; j < _gridHeight; j ++){
                    if(_dropArray[i, j]!= null){
                        if(_dropArray[i, j].IsMatched){
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        
        #endregion
    }
}