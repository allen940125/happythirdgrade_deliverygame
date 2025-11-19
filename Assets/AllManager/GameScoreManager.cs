using System;
using System.Collections.Generic;
using System.Linq;
using Gamemanager;
using UnityEngine;

public class GameScoreManager : SessionSingleton<GameScoreManager>
{
    [SerializeField] private int _currentMoney = 0; // 核心狀態：玩家目前持有的钱

    public void AddMoney(int amount)
    {
        _currentMoney += amount; // 增加 700 塊到玩家持有的總金額
    
        // 【最關鍵的一步】: 發送事件，通知所有對錢感興趣的系統
        GameManager.Instance.MainGameEvent.Send(new MoneyChangedEvent
        {
            CurrentTotalMoney = _currentMoney // 傳遞更新後的總金額
        });
    }
}
