using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.Arm;

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
       HandlePulled(); //觸發拉桿事件

        for (int i = 0; i < 15; i += 5)
          {
                Handle.Rotate(0, 0, -i);
                yield return new WaitForSeconds(0.1f);
        }
    }
    private void CheckResults()
    {
        //中三連獎
        if (Rows[0].stoppedSlot == "Diamond"
            && Rows[1].stoppedSlot == "Diamond"
            && Rows[2].stoppedSlot == "Diamond")
        {
            PrizeValue = 200;
        }
        else if (Rows[0].stoppedSlot == "Crown"
            && Rows[1].stoppedSlot == "Crown"
            && Rows[2].stoppedSlot == "Crown")
        {
            PrizeValue = 400;
        }
        else if (Rows[0].stoppedSlot == "Melon"
            && Rows[1].stoppedSlot == "Melon"
            && Rows[2].stoppedSlot == "Melon")
        {
            PrizeValue = 600;
        }
        else if (Rows[0].stoppedSlot == "Bar"
            && Rows[1].stoppedSlot == "Bar"
            && Rows[2].stoppedSlot == "Bar")
        {
            PrizeValue = 800;
        }
        else if (Rows[0].stoppedSlot == "Seven"
            && Rows[1].stoppedSlot == "Seven"
            && Rows[2].stoppedSlot == "Seven")
        {
            PrizeValue = 1500;
        }
        else if (Rows[0].stoppedSlot == "Cherry"
           && Rows[1].stoppedSlot == "Cherry"
           && Rows[2].stoppedSlot == "Cherry")
        {
            PrizeValue = 3000;
        }
        else if (Rows[0].stoppedSlot == "Lemon"
            && Rows[1].stoppedSlot == "Lemon"
            && Rows[2].stoppedSlot == "Lemon")
        {
            PrizeValue = 5000;
        }
        //中二連獎
        else if (((Rows[0].stoppedSlot == Rows[1].stoppedSlot)
            && (Rows[0].stoppedSlot == "Diamond"))

            || ((Rows[0].stoppedSlot== Rows[2].stoppedSlot)
            &&(Rows[0].stoppedSlot=="Diamond"))
            
            ||((Rows[1].stoppedSlot== Rows[2].stoppedSlot)
            &&(Rows[1].stoppedSlot=="Diamond")))    
            {
            PrizeValue = 100;
            }

        else if (((Rows[0].stoppedSlot == Rows[1].stoppedSlot)
            && (Rows[0].stoppedSlot == "Crown"))

            || ((Rows[0].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[0].stoppedSlot == "Crown"))

            || ((Rows[1].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[1].stoppedSlot == "Crown")))
            {
            PrizeValue = 300;
            }

        else if (((Rows[0].stoppedSlot == Rows[1].stoppedSlot)
            && (Rows[0].stoppedSlot == "Melon"))

            || ((Rows[0].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[0].stoppedSlot == "Melon"))

            || ((Rows[1].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[1].stoppedSlot == "Melon")))
            {
            PrizeValue = 500;
            }

        else if (((Rows[0].stoppedSlot == Rows[1].stoppedSlot)
            && (Rows[0].stoppedSlot == "Bar"))

            || ((Rows[0].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[0].stoppedSlot == "Bar"))

            || ((Rows[1].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[1].stoppedSlot == "Bar")))
            {
            PrizeValue = 700;
            }

         else if (((Rows[0].stoppedSlot == Rows[1].stoppedSlot)
            && (Rows[0].stoppedSlot == "Seven"))

            || ((Rows[0].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[0].stoppedSlot == "Seven"))

            || ((Rows[1].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[1].stoppedSlot == "Seven")))
            {
            PrizeValue = 1000;
            }

           else if (((Rows[0].stoppedSlot == Rows[1].stoppedSlot)
            && (Rows[0].stoppedSlot == "Cherry"))

            || ((Rows[0].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[0].stoppedSlot == "Cherry"))

            || ((Rows[1].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[1].stoppedSlot == "Cherry")))
            {
            PrizeValue = 2000;
            }

        else if (((Rows[0].stoppedSlot == Rows[1].stoppedSlot)
            && (Rows[0].stoppedSlot == "Lemon"))

            || ((Rows[0].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[0].stoppedSlot == "Lemon"))

            || ((Rows[1].stoppedSlot == Rows[2].stoppedSlot)
            && (Rows[1].stoppedSlot == "Lemon")))
            {
            PrizeValue = 4000;
            }
        ResultsChecked = true;
    }
}
