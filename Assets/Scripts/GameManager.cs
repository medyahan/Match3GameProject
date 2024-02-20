using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Match3Game;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [SerializeField] private Match3GameManager _match3GameManager;

    private void Start()
    {
        _match3GameManager.Initialize();
    }

    private void OnDestroy()
    {
        _match3GameManager.End();
    }
}
