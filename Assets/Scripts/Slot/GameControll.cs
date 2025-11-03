using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections;

public class GameControll : MonoBehaviour
{
    public static event Action HandlePulled = delegate { };

    [SerializeField]
    private Text PrizeText;

    [SerializeField]
    private Row[] Rows;

    [SerializeField]
    private Transform Handle;

    private int PrizeValue;

    private bool ResultsChecked = false;
    // Update is called once per frame
    void Update()
    {
        if (!Rows[0].RowStopped || !Rows[1].RowStopped || !Rows[2].RowStopped)
        {
            PrizeValue = 0;
            PrizeText.enabled = false;
            ResultsChecked = false;
        }
        if (Rows[0].RowStopped || Rows[1].RowStopped || Rows[2].RowStopped && !ResultsChecked)
        {
            CheckResults();
            PrizeText.enabled = true;
            PrizeText.text = "Prize: " + PrizeValue;
        }

    }
    private void OnMouseDown() //改手機平台測試時應更改成手機的觸控事件
    {
        if (Rows[0].RowStopped && Rows[1].RowStopped && Rows[2].RowStopped)
        {
            StartCoroutine("PullHandle");
        }
    }
    private IEnumerator PullHandle()
    {
       for(int i=0;i<15; i += 5)
        {
            Handle.Rotate(0, 0, i);
            yield return new WaitForSeconds(0.1f);
        }
       HandlePulled();

         for (int i = 0; i < 0; i += 5)
          {
                Handle.Rotate(0, 0, -i);
                yield return new WaitForSeconds(0.1f);
        }
    }
}
