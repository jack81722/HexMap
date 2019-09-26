using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnitInformation : MonoBehaviour
{
    public TextMeshProUGUI nameText, jobText, hpText, atkText, apText;
   
    public void ShowUnitInfo(IHexUnit unit)
    {
        nameText.text = unit.name;
        jobText.text = unit.Job.ToString();
        hpText.text = "HP : " + unit.HitPoint;
        atkText.text = "Atk : " + unit.Attack;
        apText.text = "AP : " + unit.ActionPoint;
    }
}
