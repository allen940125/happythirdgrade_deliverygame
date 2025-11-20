using System;
using System.Collections.Generic;
using System.Linq;
using Gamemanager;
using UnityEngine;

public class GameScoreManager : SessionSingleton<GameScoreManager>
{
    [SerializeField] private int _currentMoney = 0; // 核心狀態：玩家目前持有的錢

    // 【新增】: 暴露 CurrentMoney 供其他系統查詢
    public int CurrentMoney => _currentMoney; 

    public void AddMoney(int amount)
    {
        _currentMoney += amount; 
    
        // 【最關鍵的一步】: 發送事件，通知所有對錢感興趣的系統
        GameManager.Instance.MainGameEvent.Send(new MoneyChangedEvent
        {
            CurrentTotalMoney = _currentMoney // 傳遞更新後的總金額
        });
    }
}