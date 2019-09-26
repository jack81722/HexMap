using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SkillButton : MonoBehaviour
{
    public Image image;
    public Sprite[] icons;

    public Button button;
    public ChessManager chessMgr;

    private void Awake()
    {
        if(image == null)
            image = GetComponent<Image>();
        if (button == null)
            button = GetComponent<Button>();
    }

    public void SetJobClass(JobClass job)
    {
        //// 第一種寫法
        //if(job == JobClass.SwordMan)
        //{
        //    // do something ...
        //    image.sprite = icons[0];
        //}
        //else if(job == JobClass.Gunner)
        //{
        //    // do something ...
        //}

        //// 第二種寫法
        //switch (job)
        //{
        //    case JobClass.SwordMan:
        //        // do something ...
        //        break;
        //    case JobClass.Gunner:
        //        // do something ...
        //        break;
        //}

        // 第三種寫法
        if (image != null)
        {
            image.sprite = icons[(int)job];
            button.onClick.RemoveAllListeners();        // 刪除原本的事件

            switch (job)
            {
                case JobClass.SwordMan:
                    button.onClick.AddListener(() =>
                    {
                        chessMgr.OnClickAttackButton(1);
                    });
                    break;
                case JobClass.Gunner:
                    button.onClick.AddListener(() =>
                    {
                        chessMgr.OnClickAttackButton(3);
                    });
                    break;
            }
        }
        }
}
