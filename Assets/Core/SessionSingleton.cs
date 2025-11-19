// 【GameQuestManager 應繼承的版本】

using UnityEngine;

public abstract class SessionSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // 專門用於本局遊戲的單例
    protected static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                // **【關鍵修改】** 不允許自動創建！
                _instance = FindObjectOfType<T>();
                
                if (_instance == null)
                {
                    // 如果找不到，則視為設計錯誤！
                    Debug.LogError($"[SessionSingleton<{typeof(T)}>] 錯誤！試圖調用場景單例 '{typeof(T).Name}' 但它不存在於當前場景中。");
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // 處理場景中有多個實例的問題
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        // **【關鍵修改】** 移除 DontDestroyOnLoad(gameObject);
        
        // 通常在這裡初始化本局資料
        // InitializeRun(); 
    }
}