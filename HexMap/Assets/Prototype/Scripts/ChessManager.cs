using Cinemachine;
using HexGame.Packets;
using Network.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public partial class ChessManager : MonoBehaviour, IChessManager
{
    public HexGrid hexGrid;
    public Chess chessPrefab;
    public Material Enemy_Material;
    public CinemachineBrain cameraBrain;

    // 被選擇的棋子
    private Coroutine movingCoroutine;
    private List<HexCell> path;

    private List<IHexUnit> unitQueue;
    public IHexUnit CurrentUnit { get; private set; }
    private List<HexCell> currentUnitReachableCells;        // 可移動到的所有地圖格

    public HeadQueue headQueue;
    public SkillList skillList;

    public LineRenderer line;
    public GameObject pointPrefab;
    private List<GameObject> points = new List<GameObject>();

    public UnitInformation unitInfo;

    private Dictionary<int, IPlayer> players = new Dictionary<int, IPlayer>();

    public List<ChessAbility> chessAbilities;

    private void Start()
    {   
        unitQueue = new List<IHexUnit>();
        currentUnitReachableCells = new List<HexCell>();

        HexCoordinate center = new HexCoordinate(hexGrid.cellCountX / 8, hexGrid.cellCountZ / 8);
        
        List<HexCell> groupACells = new List<HexCell>(hexGrid.GetCellsInRange(hexGrid.GetCell(center), 3, (cell) => !cell.IsUnderwater));
        Shuffle(groupACells);

        for (int i = 0; i < chessAbilities.Count; i++)
        {
            var chess = CreateUnit(groupACells[i], chessAbilities[i]);
            unitQueue.Add(chess);
            _units.Add(chess.Id, chess);
        }

        refreshActionQueue();

    }

    public Chess CreateUnit(HexCell cell, ChessAbility ability)
    {   
        Chess chess = Instantiate(chessPrefab);
        
        chess.transform.SetParent(transform);
        chess.Grid = hexGrid;
        chess.Location = cell;

        chess.SetAbility(ability);

        return chess;
    }

    public void DestroyUnit(IHexUnit unit)
    {
        unit.transform.gameObject.SetActive(false);     // 隱藏該棋子
        unit.Location.iUnit = null;                     // 格子上抹除棋子的資料
    }
    
    public void RegisterPlayer(IPlayer player)
    {
        if (!players.ContainsKey(player.Group))
        {
            players.Add(player.Group, player);
        }
        else
        {
            Debug.LogError($"Specific player of group[{player.Group}] has been existed.");
        }
    }

    public void UnegisterPlayer(IPlayer player)
    {
        players.Remove(player.Group);
    }

    //private void Update()
    //{   
    //    HexCell cell = hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition), out cursorPosition);  // 先找到指標只到的地圖格
    //    if (Input.GetMouseButtonDown(0))
    //    {   
    //        if (cell != null && cell.iUnit != null)
    //        {
    //            // select
    //            SelectUnit(cell.iUnit);
    //        }
    //        else
    //        {
    //            // clear
    //            CancelSelect();
    //            CancelAim();
    //        }
    //    }
    //    else if (Input.GetMouseButtonDown(1))
    //    {
    //        // 點擊右鍵

    //        if (attackAim)
    //        {
    //            if (currentUnitAttackableCells.Contains(cell))    // 確認該格子是否在可攻擊的格子群(array)中
    //            {
    //                // 進行攻擊
    //                attack(CurrentUnit, cell.iUnit);
    //                CurrentUnit.ActionPoint = 0;
    //                refreshActionQueue();
    //            }
    //        }
    //        else
    //        {
    //            if (selectedUnit == CurrentUnit && 
    //                currentUnitReachableCells.Contains(cell) && cell.iUnit == null)
    //            {
    //                // 執行移動指令
    //                int moveCost = Move(selectedUnit, cell, (cost) =>
    //                {
    //                    // 扣除移動的行動點數
    //                    if (cost >= 0)
    //                        selectedUnit.ActionPoint -= cost;
    //                    else
    //                    {
    //                        // do something ...
    //                    }
    //                    // 判斷是否有剩餘的行動點數
    //                    if (selectedUnit.ActionPoint <= 0)
    //                    {
    //                        // 耗完行動點數則重新整理行動排序
    //                        refreshActionQueue();
    //                    }
    //                });

    //            }
    //        }
    //    }
    //    else
    //    {
    //        cursorCell = cell;
    //        if(selectedUnit == CurrentUnit && !attackAim)
    //        {
    //            markReachableCells(cell);
    //        }
    //        else if(selectedUnit == CurrentUnit && attackAim && cell != null)
    //        {
    //            showTrajectory(selectedUnit.Location, cell, selectedSkill);
    //        }
    //    }
    //}

    private void SetSkillBtnList(IHexUnit unit, bool interactable, Action<ISkill> action)
    {
        skillList.Refresh(unit.GetSkills(), interactable, action);
    }

    private void ShowUnitInfo(IHexUnit unit)
    {
        if (unitInfo != null)
            unitInfo.ShowUnitInfo(unit);
    }

    // 進行移動並回傳移動所消耗的行動點數 (Action<int> => void Func(int n))
    private int Move(IHexUnit unit, HexCell goal, Action<int> afterMove)
    {
        hexGrid.FindPath(unit.Location, goal, unit.Speed);
        path = hexGrid.GetPath();
        // move
        if (path != null && path.Count > 1)
        {
            int sumCost = 0;
            for (int i = 0; i < path.Count - 1; i++)
            {
                int cost = hexGrid.GetMoveCost(path[i], path[i + 1], path[i].GetNeighborDirection(path[i + 1]));
                sumCost += cost;
            }
            movingCoroutine = StartCoroutine(Moving(unit, path, sumCost, afterMove));
            return sumCost;
        }
        return -1;
    }

    private IEnumerator Moving(IHexUnit unit, List<HexCell> path, int moveCost, Action<int> afterMove)
    {
        float animeSpd = 3f;
        float animeTime = 1f / animeSpd;
        int p = 1;
        HexCell next;
        Vector3 pos = unit.Position;
        float timer = 0;
        while (p < path.Count)
        {
            next = path[p];
            timer += Time.deltaTime;
            unit.Position = Vector3.Lerp(pos, next.Position, timer / animeTime);
            yield return null;
            if (timer >= animeTime)
            {
                unit.Location = next;
                pos = next.Position;
                next = path[p];
                timer = 0;
                p++;
            }
        }
        hexGrid.ClearPath();
        movingCoroutine = null;
        // 正規寫法
        //if (afterMove != null)
        //    afterMove.Invoke(moveCost);
        // 語法糖寫法
        afterMove?.Invoke(moveCost);
        ShowUnitInfo(unit);
    }

    private void refreshActionQueue()
    {
        // 排序最快滿進度條的棋子
        unitQueue.Sort((unitA, unitB) =>
        {
            return remain(unitA) - remain(unitB);
        });
        // 重新整理頭像順序
        refreshHeadQueue(unitQueue);

        // 指定滿進度條的棋子為行動者
        setChessAction(unitQueue[0]);
        // 扣除行動條值
        float time = remainTime(CurrentUnit);
        for (int i = 0; i < unitQueue.Count; i++)
        {
            IHexUnit unit = unitQueue[i];
            unit.ActionProcess = (unit.ActionProcess + time * unit.Agility);
        }
        // 行動值滿後-100
        CurrentUnit.ActionProcess %= 100;


        // remain time of next action
        int remain(IHexUnit unit)
        {
            if (!unit.IsAlive)
                return int.MaxValue;
            return (int)((100 - unit.ActionProcess) / unit.Agility) * 100;
        }

        float remainTime(IHexUnit unit)
        {
            return (100 - unit.ActionProcess) / unit.Agility;
        }
    }

    // 指定當前回合行動的角色
    private void setChessAction(IHexUnit unit)
    {
        if (players.TryGetValue(unit.Group, out IPlayer player))
        {
            player.AwakeWithUnit(unit);
        }
        else
        {
            Debug.LogError($"Specific player of group[{unit.Group}] not found.");
        }
        foreach (var c in currentUnitReachableCells)
        {
            c.DisableHightlight();
        }
        CurrentUnit = unitQueue[0];
        CurrentUnit.ActionPoint = 20;       // 給予角色行動點數
        cameraBrain.ActiveVirtualCamera.LookAt = CurrentUnit.transform;
        cameraBrain.ActiveVirtualCamera.Follow = CurrentUnit.transform;
        // 取得可移動的範圍
        currentUnitReachableCells = hexGrid.GetReachableCells(CurrentUnit.Location,CurrentUnit.Speed);
    }

    // 重新整理頭像排序
    private void refreshHeadQueue(IList<IHexUnit> queue)
    {
        headQueue.RefreshQueue(queue, null);
    }

    private void Shuffle<T>(List<T> list)
    {   
        for(int i = 0; i < list.Count - 1; i++)
        {
            int rindex = UnityEngine.Random.Range(i + 1, list.Count - 1);
            T temp = list[i];
            list[i] = list[rindex];
            list[rindex] = temp;
        }
    }

    public bool DoSkill(IHexUnit attacker, HexCell location, ISkill skill)
    {
        bool result = false;
        int groupMask = -1 ^ attacker.Group;
        List<HexCell> inRange;
        switch (skill.SkillRangeType)
        {
            case ESKillRangeType.Single:
                if (ValidateTarget(CurrentUnit, location.iUnit, groupMask))          
                {
                    attack(attacker, location.iUnit);
                }
                break;
            case ESKillRangeType.Circle:
                inRange = hexGrid.GetCellsInRange(location, skill.HitRange);
                foreach(var cell in inRange)
                {
                    if(ValidateTarget(CurrentUnit, cell.iUnit, groupMask))
                    {
                        attack(attacker, cell.iUnit);
                    }
                }
                break;
            case ESKillRangeType.Line:
                hexGrid.FindPath(attacker.Location, location, skill.HitRange);
                inRange = hexGrid.GetPath();
                foreach (var cell in inRange)
                {
                    if (ValidateTarget(CurrentUnit, cell.iUnit, groupMask))
                    {
                        attack(attacker, cell.iUnit);
                    }
                }
                break;
        }
        return result;
    }

    public bool ValidateTarget(IHexUnit attacker, IHexUnit defender, int groupMask = -1, bool isAlive = true)
    {
        
        return defender != null && 
            (defender.Group & groupMask) != 0 &&
            (defender.IsAlive && isAlive);
    }

    private void attack(IHexUnit attacker, IHexUnit defender)
    {
        defender.HitPoint -= attacker.Attack;
        if (!defender.IsAlive)
            DestroyUnit(defender);
    }
}

/// <summary>
/// Part of server
/// </summary>
public partial class ChessManager : IOperationHandler
{
    Dictionary<int, IHexUnit> _units = new Dictionary<int, IHexUnit>();

    public bool CheckTurn(IHexUnit unit)
    {
        return unit == CurrentUnit;
    }

    public bool CheckMove(IHexUnit unit, HexCell goal)
    {
        if (!CheckTurn(unit))
            return false;
        hexGrid.FindPath(unit.Location, goal, unit.Speed);
        path = hexGrid.GetPath();
        return path != null && path.Count > 1;
    }

    public byte OperationCode => (byte)EOperationCode.GameLogic;

    public void Handle(IPeer peer, IDictionary<byte, object> payload)
    {
        foreach (var pair in payload)
        {
            switch (pair.Key)
            {
                case (byte)EGameSwitchCode.AttackRequest:
                    break;
                case (byte)EGameSwitchCode.MoveRequest:
                    onReceiveMoveReq(peer, (MoveCommand)pair.Value);
                    break;

            }
        }
    }

    public void UpdateHandler(float deltaTime)
    {
        
    }

    public void OnPeerConnected(IPeer peer)
    {

    }

    public void OnPeerDisconnected(IPeer peer)
    {

    }

    private void onReceiveMoveReq(IPeer peer, MoveCommand packet)
    {
        int unitId = packet.UnitId;
        int cellId = packet.CellId;
        if (CheckMove(_units[unitId], hexGrid.GetCell(cellId)))
        {
            peer.Send(
                OperationCode,
                new Dictionary<byte, object>
                {
                    {(byte)EGameSwitchCode.MoveCommand, new MoveCommand{UnitId = unitId, CellId = cellId} }
                },
                Network.Shared.EReliability.ReliableOrdered);
            Debug.Log("Send packet");
        }
        else
        {
            Debug.LogError($"Unit[{unitId}] cannot move to specific cell.");
        }
    }
}

/// <summary>
/// Part of client
/// </summary>
public partial class ChessManager
{
    public int DoMove(int unitId, int cellId)
    {
        return Move(_units[unitId], hexGrid.GetCell(cellId), (cost) =>
        {
            // 扣除移動的行動點數
            if (cost >= 0)
                _units[unitId].ActionPoint -= cost;
            else
            {
                // do something ...
            }
            // 判斷是否有剩餘的行動點數
            if (_units[unitId].ActionPoint <= 0)
            {
                // 耗完行動點數則重新整理行動排序
                refreshActionQueue();
            }
        });
    }

    public void DoAttack(int attacker, int defender, SkillInfo skill)
    {
        // 進行攻擊
        attack(_units[attacker], _units[defender]);
        CurrentUnit.ActionPoint = 0;
        refreshActionQueue();
    }
    
    public void FocusOnUnit(IHexUnit unit, bool interactable)
    {
        cameraBrain.ActiveVirtualCamera.LookAt = unit.transform;
        cameraBrain.ActiveVirtualCamera.Follow = unit.transform;

        if (players.TryGetValue(unit.Group, out IPlayer player))
        {
            ShowUnitInfo(unit);
            SetSkillBtnList(unit, interactable, player.OnSelectSkill);
        }
    }
}

public interface IChessManager
{

}

