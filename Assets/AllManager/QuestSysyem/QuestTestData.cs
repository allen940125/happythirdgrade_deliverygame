// 测试用任务数据生成器 (TestQuestData.cs)
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "QuestTestData", menuName = "Test/QuestTestData")]
public class QuestTestData : ScriptableObject
{
    public List<QuestData> testQuests = new List<QuestData>
    {
        new QuestData
        {
            questID = 101,
            title = "失落的典籍",
            status = QuestStatus.NotStarted
        },
        new QuestData
        {
            questID = 102,
            title = "药剂师的请求",
            status = QuestStatus.NotStarted
        },
        new QuestData
        {
            questID = 201,
            title = "禁忌的知识",
            status = QuestStatus.NotStarted
        }
    };
}

// 在Editor文件夹下创建
[CustomEditor(typeof(QuestTestData))]
public class QuestTestDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        // 仅在 Editor 模式下显示按钮
        if (Application.isPlaying) 
        {
            if (GUILayout.Button("【运行时】注入测试数据"))
            {
                var data = (QuestTestData)target;
                
                // 1. 检查 GameQuestManager 是否已存在于场景
                var questManager = GameQuestManager.Instance;
                
                if (questManager != null)
                {
                    // 2. 调用初始化方法
                    questManager.InitializeRun(data.testQuests);
                    Debug.Log("已通过 Editor 按钮注入测试任务数据");
                }
                else
                {
                    Debug.LogError("场景中找不到 GameQuestManager 实例，请确保它已挂载到 GameSession 上。");
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("此按钮仅在游戏运行时有效。", MessageType.Info);
        }
    }
}
#endif