using System.Collections;
using UnityEngine;

public class Row : MonoBehaviour
{
    private int RandomValue;
    private float TimeInterval;

    public bool RowStopped;
    public string stoppedSlot;

    //Use this for initialization
    void Start()
    {
        RowStopped = true;
        GameControll.HandlePulled += StartRotating; //訂閱拉桿事件
    }

    private void StartRotating()
    {
        stoppedSlot = "";
        StartCoroutine("Rotate");
    }

    private IEnumerator Rotate()
    {
        RowStopped = false;
        TimeInterval = 0.025f;

        for (int i = 0; i < 30; i++)
        {
            if (transform.position.y <= -0.5F) //f設定地一張
            {
                transform.position = new Vector2(transform.position.x, 20.5f);//f設定地一張 造成循環
            }
            transform.position = new Vector2(transform.position.x, transform.position.y - 3); //我的圖片是每小格-3f 而不是-0.25f 共三格 之後改

            yield return new WaitForSeconds(TimeInterval);
        } //開始旋轉
        RandomValue = Random.Range(60, 100);

        switch (RandomValue % 3)
        {
            case 1:
                RandomValue += 2;
                break;
            case 2:
                RandomValue += 1;
                break;
        } //確保停在0格 把餘數轉成0
        for (int i = 0; i < RandomValue; i++)
        {
            if (transform.position.y <= -0.5f) //f設定地一張
            {
                transform.position = new Vector2(transform.position.x, 20.5f);//f設定地一張 造成循環
            }
            transform.position = new Vector2(transform.position.x, transform.position.y - 3); //我的圖片是每小格-3f 而不是-0.25f 共三格 之後改

            if (i > Mathf.RoundToInt(RandomValue * 0.25f)) 
            {
                TimeInterval= 0.05f;
            }
            if (i > Mathf.RoundToInt(RandomValue * 0.5f))
            {
                TimeInterval = 0.1f;
            }
            if (i > Mathf.RoundToInt(RandomValue * 0.75f))
            {
                TimeInterval = 0.15f;
            }
            if (i > Mathf.RoundToInt(RandomValue * 0.95f))
            {
                TimeInterval = 0.2f;
            }

            yield return new WaitForSeconds(TimeInterval);

        } //開始讓轉盤慢下來

        if (transform.position.y == -0.5f) //每個格子的位置
        {
            stoppedSlot = "Diamond";
        }
        else if (transform.position.y == 2.5f)
        {
            stoppedSlot = "Crown";
        }
        else if (transform.position.y == 5.5f)
        {
            stoppedSlot = "Melon";
        }
        else if (transform.position.y == 8.5f)
        {
            stoppedSlot = "Bar";
        }
        else if (transform.position.y == 11.5f)
        {
            stoppedSlot = "Seven";
        }
        else if (transform.position.y == 14.5f)
        {
            stoppedSlot = "Cherry";
        }
        else if (transform.position.y == 17.5f)
        {
            stoppedSlot = "Lemon";
        }
        else if (transform.position.y == 20.5f)
        {
            stoppedSlot = "Diamond";
        } 
        RowStopped = true;
        //判斷停在哪個圖案上

    }
    private void OnDestroy()
    {
        GameControll.HandlePulled -= StartRotating; //取消訂閱拉桿事件 避免換場景或物件被刪除時沒取消發生錯誤
    }
}
