using System.Collections;
using Core;
using Data;
using DG.Tweening;
using Enum;
using Match3Game.GameBoard;
using UnityEngine;

namespace Match3Game.Drop
{
    public class Drop : BaseMonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _swapDuration;
        [SerializeField] private float _destroyAnimateDuration;
    
        public int Column { get; set; }
        public int Row { get; set; }

        private float _swipeAngle;
        private float _swipeResist = .5f;
        private Vector2 _startTouchPos;
        private Vector2 _endTouchPos;
        private bool _isMatched;
        private Drop _targetDrop;

        private DropData _data;
        public bool IsMatched => _isMatched;
        public DropData Data => _data;

        public override void Initialize(params object[] list)
        {
            base.Initialize(list);

            SetData((DropData) list[0]);

            transform.localScale = Vector3.one;
        }

        public override void End()
        {
            base.End();

            AnimateDestroying();
        }
        
        private void AnimateDestroying()
        {
            transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), _destroyAnimateDuration/2f).OnComplete((() =>
            {
                transform.DOScale(Vector3.zero, _destroyAnimateDuration/2f).OnComplete((() =>
                {
                    _isMatched = false;
                    _spriteRenderer.color = Color.white;
                }));
            }));
        }
        
        private void Update()
        {
            CheckNeighboringMatches();
        
            if (_isMatched)
            {
                _spriteRenderer.color = Color.black;
            }
        }

        /// <summary>
        /// Sets the data for the Drop using the provided DropData.
        /// </summary>
        /// <param name="dropData">The DropData to set.</param>
        private void SetData(DropData dropData)
        {
            _data = dropData;
            _spriteRenderer.sprite = _data.Sprite;
        }

        /// <summary>
        /// Moves the Drop to the specified grid column and row over a specified duration using DOTween.
        /// </summary>
        /// <param name="column">The target grid column.</param>
        /// <param name="row">The target grid row.</param>
        /// <param name="duration">The duration of the movement animation.</param>
        public void MoveTo(int column, int row, float duration)
        {
            transform.DOMove(new Vector2(column, row), duration);
        }
        
        #region MOUSE METHODS

        /// <summary>
        /// Handles the mouse down event for initiating a swipe.
        /// </summary>
        private void OnMouseDown()
        {
            if(GameBoardManager.Instance.CurrentGameState == GameStateType.Waiting) return;
            
            _startTouchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        /// <summary>
        /// Handles the mouse up event for completing a swipe and triggering drop swapping.
        /// </summary>
        private void OnMouseUp()
        {
            if(GameBoardManager.Instance.CurrentGameState == GameStateType.Waiting) return;
            
            _endTouchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition); 
            
            if (Mathf.Abs(_endTouchPos.y - transform.position.y) > _swipeResist ||
                Mathf.Abs(_endTouchPos.x - transform.position.x) > _swipeResist)
            {
                CalculateAngle();
                SwapDrops();
                GameBoardManager.Instance.CurrentGameState = GameStateType.Waiting;
            }
            else
                GameBoardManager.Instance.CurrentGameState = GameStateType.Continue;
        }

        #endregion

        #region SWAP TRANSACTIONS
    
        /// <summary>
        /// Calculates the angle of the swipe gesture.
        /// </summary>
        private void CalculateAngle()
        {
            _swipeAngle = Mathf.Atan2(_endTouchPos.y - _startTouchPos.y, _endTouchPos.x - _startTouchPos.x) * 180 / Mathf.PI;
        }
    
        /// <summary>
        /// Swaps the current drop with the adjacent drop based on the swipe angle.
        /// </summary>
        private void SwapDrops()
        { 
            // Right Swipe
            if(_swipeAngle > -45 && _swipeAngle <= 45 && Column < GameBoardManager.Instance.GridWidth - 1){
                _targetDrop = GameBoardManager.Instance.DropArray[Column + 1, Row];
                SwapPosition(_targetDrop);
            } 
            // Up Swipe
            else if(_swipeAngle > 45 && _swipeAngle <= 135 && Row < GameBoardManager.Instance.GridHeight - 1){
            
                _targetDrop = GameBoardManager.Instance.DropArray[Column, Row + 1];
                SwapPosition(_targetDrop);
            } 
            // Left Swipe
            else if((_swipeAngle > 135 || _swipeAngle <= -135) && Column > 0){
            
                _targetDrop = GameBoardManager.Instance.DropArray[Column - 1, Row];
                SwapPosition(_targetDrop);
            } 
            // Down Swipe
            else if(_swipeAngle < -45 && _swipeAngle >= -135 && Row > 0){
            
                _targetDrop = GameBoardManager.Instance.DropArray[Column, Row - 1];
                SwapPosition(_targetDrop);
            }
        
            StartCoroutine(HandleSwapResult());
        }
        
        /// <summary>
        /// Swaps the positions of the current drop with the target drop.
        /// </summary>
        /// <param name="targetDrop">The target drop to swap positions with.</param>
        private void SwapPosition(Drop targetDrop)
        {
            int tempRow = targetDrop.Row;
            int tempColumn = targetDrop.Column;

            targetDrop.Row = Row;
            targetDrop.Column = Column;
            GameBoardManager.Instance.DropArray[Column, Row] = targetDrop;
            targetDrop.MoveTo(Column, Row, _swapDuration);
        
            Row = tempRow;
            Column = tempColumn;
            GameBoardManager.Instance.DropArray[tempColumn, tempRow] = this;
            
            MoveTo(tempColumn, tempRow, _swapDuration);
        }

        /// <summary>
        /// Handles the result of the swap operation, checking for matches and initiating further actions.
        /// </summary>
        private IEnumerator HandleSwapResult()
        {
            yield return new WaitForSeconds(.5f);
        
            if(_targetDrop != null)
            {
                if (!_isMatched && !_targetDrop._isMatched)
                {
                    SwapPosition(_targetDrop);
                    yield return new WaitForSeconds(.5f);
                    GameBoardManager.Instance.CurrentGameState = GameStateType.Continue;
                }
                else
                    GameBoardManager.Instance.DestroyMatchedDrops();

                _targetDrop = null;
            }
           
        }
    
        #endregion

        /// <summary>
        /// Checks for neighboring matches in horizontal and vertical directions.
        /// </summary>
        private void CheckNeighboringMatches()
        {
            // Check horizontal matches
            if (Column > 0 && Column < GameBoardManager.Instance.GridWidth - 1)
            {
                Drop leftDrop = GameBoardManager.Instance.DropArray[Column - 1, Row];
                Drop rightDrop = GameBoardManager.Instance.DropArray[Column + 1, Row];

                if (leftDrop != null && rightDrop != null)
                {
                    if (leftDrop._data == _data && rightDrop._data == _data)
                    {
                        leftDrop._isMatched = true;
                        rightDrop._isMatched = true;
                        _isMatched = true;
                    }
                }
            }
            // Check vertical matches
            if (Row > 0 && Row < GameBoardManager.Instance.GridHeight - 1)
            {
                Drop upDrop = GameBoardManager.Instance.DropArray[Column, Row + 1];
                Drop downDrop = GameBoardManager.Instance.DropArray[Column, Row - 1];

                if(upDrop != null && downDrop != null)
                {
                    if (upDrop._data == _data && downDrop._data == _data)
                    {
                        upDrop._isMatched = true;
                        downDrop._isMatched = true;
                        _isMatched = true;
                    }
                }
            }
        }
    
    }
}