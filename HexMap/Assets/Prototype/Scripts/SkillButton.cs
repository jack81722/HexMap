using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    public Image image;
    public RectTransform rect;

    public Button button;
    public ChessManager chessMgr;

    public IHexUnit Owner { get; private set; }

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        button = GetComponent<Button>();
    }

    public void SetSkill(ISkill skill, Action<ISkill> action)
    {
        image.sprite = skill.SkillIcon;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            action?.Invoke(skill);
        });
    }
}
