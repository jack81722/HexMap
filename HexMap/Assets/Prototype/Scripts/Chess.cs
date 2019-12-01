using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chess : MonoBehaviour, IHexUnit
{
    public int Id => GetInstanceID();
    public int Group { get; set; }
    public JobClass Job { get { return chessAbility.Job; } }

    #region Abilities
    public ChessAbility chessAbility;

    [SerializeField] private int hitPoint;
    public int HitPoint
    {
        get { return hitPoint; }
        set
        {
            hitPoint = value;
            if (hitPoint < 0)
                hitPoint = 0;
            if (hitPoint > chessAbility.MaxHitPoint)
                hitPoint = chessAbility.MaxHitPoint;
        }
    }
    [SerializeField] private int attack;
    public int Attack { get { return attack; } set { attack = value; } }
    [SerializeField] private int speed;
    public int Speed { get { return speed; } set { speed = value; } }
    [SerializeField] private float agility;
    public float Agility { get { return agility; } set { agility = value; } }
    [SerializeField] private int visionRange;
    public int VisionRange { get { return visionRange; } set { visionRange = value; } }
    #endregion

    public HexGrid Grid { get; set; }
    private HexCell location;
    public HexCell Location
    {
        get { return location; }
        set
        {
            if (location)
            {
                Grid.DecreaseVisibility(location, VisionRange);
                location.Unit = null;
                if(location.iUnit == this)
                    location.iUnit = null;
            }
            location = value;
            if(location.iUnit == null)
                location.iUnit = this;
            Grid.IncreaseVisibility(value, VisionRange);
            Position = value.Position;
        }
    }

    private bool positionChanged;
    private Vector3 position;
    public Vector3 Position
    {
        get
        {   
            return position;
        }
        set
        {
            positionChanged = true;
            position = value;
        }
    }
    
    public float actionProcess;
    public float ActionProcess { get { return actionProcess; } set { actionProcess = value; } }

    [SerializeField]
    private int actionPoint;
    public int ActionPoint { get { return actionPoint; } set { actionPoint = value; } }

    public bool IsAlive { get { return hitPoint > 0; } }

    [SerializeField]
    private List<SkillData> skills;

    public void Start()
    {
        ResetAbility();
    }

    public void SetAbility(ChessAbility ability)
    {
        chessAbility = ability;
        ResetAbility();
    }

    public void ResetAbility()
    {
        name = chessAbility.Name;
        HitPoint = chessAbility.MaxHitPoint;
        Attack = chessAbility.Attack;
        Speed = chessAbility.Speed;
        Agility = chessAbility.Agility;
        VisionRange = chessAbility.VisionRange;

        skills = chessAbility.skills;
    }

    private void LateUpdate()
    {
        if (positionChanged)
            transform.localPosition = Position;
    }

    public IList<ISkill> GetSkills()
    {
        List<ISkill> list = new List<ISkill>(skills.Count);
        foreach(var skill in skills)
        {
            list.Add(skill);
        }
        return list;
    }
}
