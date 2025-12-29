using UnityEngine;
using System.Collections.Generic;

public class TrafficManager : MonoBehaviour
{
    public Transform player;
    public GameObject trafficCarPrefab; // 路人車 Prefab
    public List<TrafficNode> allNodes; // 場景中所有的路點 (可以用 FindObjectsOfType 抓)

    [Header("生成設定")]
    public int maxTrafficCars = 20;
    public float spawnDistance = 80f; // 在多遠的地方生車
    public float despawnDistance = 150f; // 多遠刪車

    private List<GameObject> activeCars = new List<GameObject>();
    private float timer;

    void Start()
    {
        // 抓取場景所有路點 (耗效能，只做一次)
        allNodes = new List<TrafficNode>(FindObjectsOfType<TrafficNode>());
    }

    void Update()
    {
        if (player == null) return;

        // 1. 回收太遠的車
        for (int i = activeCars.Count - 1; i >= 0; i--)
        {
            float dist = Vector3.Distance(player.position, activeCars[i].transform.position);
            if (dist > despawnDistance)
            {
                Destroy(activeCars[i]); // 正式版請改用 ObjectPool.Return()
                activeCars.RemoveAt(i);
            }
        }

        // 2. 補車 (如果數量不夠)
        if (activeCars.Count < maxTrafficCars)
        {
            TrySpawnCar();
        }
    }

    void TrySpawnCar()
    {
        // 隨機找一個路點
        TrafficNode randomNode = allNodes[Random.Range(0, allNodes.Count)];

        // 檢查這個點是否在玩家附近的「生成圈」內
        float dist = Vector3.Distance(player.position, randomNode.transform.position);
        
        // 條件：距離適中 (不要太近嚇到玩家，也不要太遠看不到)
        if (dist > spawnDistance && dist < despawnDistance)
        {
            // 生成車子 (正式版請改用 ObjectPool.Get())
            GameObject newCar = Instantiate(trafficCarPrefab, randomNode.transform.position, randomNode.transform.rotation);
            
            // 初始化 AI
            newCar.GetComponent<TrafficCarAI>().currentNode = randomNode;
            
            activeCars.Add(newCar);
        }
    }
}