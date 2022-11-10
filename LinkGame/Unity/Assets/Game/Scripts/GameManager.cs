using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Framework;
using System;

[Serializable]
public enum GAME_MODE
{
    EASY,
    NORMAL,
    HARD
}

public class GameManager : SingletonComponent<GameManager>
{
    public List<Sprite> cellIconList = new List<Sprite>();

    private GAME_MODE _gameMode = GAME_MODE.EASY;

    void Start()
    {
        SoundManager.Instance.Play("bg");
    }

    public void EnterGame(string mode)
    {
        _gameMode = GAME_MODE.EASY;
        ScreenManager.Instance.Show("game");
    }
}
