using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SkillList : MonoBehaviour
{
    public float padding;
    RectTransform rect;
    public ChessManager chessMgr;
    public SkillButton skillBtnPrefab;
    public List<SkillButton> skillBtnList;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (skillBtnList == null) skillBtnList = new List<SkillButton>();
    }

    public void Refresh(IList<ISkill> skills, bool interactable, Action<ISkill> action)
    {
        if (skillBtnList.Count != skills.Count)
            ResizeBtnList(skills.Count);
        for(int i = 0; i < skills.Count; i++)
        {
            skillBtnList[i].SetSkill(skills[i], action);
            skillBtnList[i].button.interactable = interactable;
        }
    }

    public void ResizeBtnList(int skillCount)
    {
        if(skillBtnList.Count < skillCount)
        {
            int count = skillCount - skillBtnList.Count;
            for(int i = 0; i < count; i++)
            {
                var btn = Instantiate(skillBtnPrefab, transform);
                skillBtnList.Add(btn);
            }
        }
        else if(skillBtnList.Count > skillCount)
        {
            int count = skillBtnList.Count - skillCount;
            foreach(var skillBtn in skillBtnList.GetRange(skillBtnList.Count - 1 - count, count))
            {
                Destroy(skillBtn.gameObject);
            }
            skillBtnList.RemoveRange(skillBtnList.Count - 1 - count, count);
        }

        float skillBtnWidth = skillBtnPrefab.GetComponent<RectTransform>().rect.width;
        float totalWidth = (skillBtnList.Count - 1) * padding + skillBtnList.Count * skillBtnWidth;
        for(int i = 0; i < skillBtnList.Count; i++)
        {
            skillBtnList[i].rect.position = new Vector2(rect.position.x - totalWidth / 2 + skillBtnWidth / 2 + skillBtnWidth * i + padding * i, rect.position.y);
            skillBtnList[i].chessMgr = chessMgr;
        }
    }
}
