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
            if (transform.position.y <= 3.5f) //f設定地一張
            {
                transform.position = new Vector2(transform.position.x, 1.75f);//f設定地一張 造成循環
            }
            transform.position = new Vector2(transform.position.x, transform.position.y - 0.25f); //我的圖片是每小格-1f 而不是-0.25f 共三格 之後改

            yield return new WaitForSeconds(TimeInterval);
        }
        RandomValue = Random.Range(60, 100);

        switch (RandomValue % 3)
        {
            case 1:
                RandomValue += 2;
                break;
            case 2:
                RandomValue += 1;
                break;
        }

    }
}
