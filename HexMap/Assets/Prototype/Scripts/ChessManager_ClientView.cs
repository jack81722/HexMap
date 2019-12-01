using HexGame.Packets;
using Network.Client;
using Network.Interfaces;
using Network.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessManager_ClientView : MonoBehaviour, IOperationHandler, IPlayer
{
    public IPeer Peer { get; private set; }

    [SerializeField]
    private int group;
    public int Group => group;
    public ChessManager mChessMgr;

    public HexGrid hexGrid;
    private HexCell cursorCell;
    private Vector3 cursorPosition;

    // 被選擇的棋子
    private IHexUnit selectedUnit;
    private List<HexCell> path;

    private List<HexCell> currentUnitReachableCells = new List<HexCell>();        // 可移動到的所有地圖格

    // 瞄準狀態
    private bool attackAim;
    private ISkill selectedSkill;
    private List<HexCell> currentUnitAttackableCells = new List<HexCell>();

    #region -- Unity APIs --
    private void Start()
    {
        mChessMgr.RegisterPlayer(this);
    }

    private void Update()
    {
        HexCell cell = hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition), out cursorPosition);  // 先找到指標只到的地圖格
        if (Input.GetMouseButtonDown(0))
        {
            if (cell != null && cell.iUnit != null)
            {
                // select
                SelectUnit(cell.iUnit);
            }
            else
            {
                // clear
                CancelSelect();
                CancelAim();
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            // 點擊右鍵
            if (attackAim)
            {
                if (!currentUnitAttackableCells.Contains(cell))    // 確認該格子是否在可攻擊的格子群(array)中
                {
                    Debug.LogError("Specific cell is not in attack range.");
                }
                else if(cell.iUnit == null)
                {
                    Debug.LogError("No target entity on the cell.");
                }
                else
                { 
                    // 進行攻擊
                    RequestAttack(mChessMgr.CurrentUnit.Id, cell.iUnit.Id);
                }
            }
            else
            {
                if (mChessMgr.CheckTurn(selectedUnit))
                {
                    if (currentUnitReachableCells.Contains(cell) && cell.iUnit == null)
                    {
                        RequestMove(selectedUnit.Id, cell.Index);
                    }
                    else
                    {
                        Debug.LogError($"The specific cell is not in reachable cells.");
                    }
                }
                else
                {
                    Debug.LogError($"Not turn of unit[{selectedUnit.Id}] now.");
                }
            }
        }
        else
        {
            cursorCell = cell;
            if (selectedUnit == mChessMgr.CurrentUnit && !attackAim)
            {
                markReachableCells(cell);
            }
            else if (selectedUnit == mChessMgr.CurrentUnit && attackAim && cell != null)
            {
                showTrajectory(selectedUnit.Location, cell, selectedSkill);
            }
        }
    }
    #endregion

    public void AwakeWithUnit(IHexUnit unit)
    {
        SelectUnit(unit);
        // 取得可移動的範圍
        currentUnitReachableCells = hexGrid.GetReachableCells(unit.Location, unit.Speed);
    }

    private void showTrajectory(HexCell fromCell, HexCell toCell, ISkill skill)
    {
        switch (skill.SkillRangeType)
        {
            case ESKillRangeType.Single:
                currentUnitAttackableCells = hexGrid.GetCellsInRange(fromCell, skill.AimDistance, (cell) => !cell.IsUnderwater, true);
                markCells(currentUnitAttackableCells, Color.red, new Color(1f, 0, 0, 0.4f),
                    (cell) => cursorCell == cell && cell.iUnit != mChessMgr.CurrentUnit, (cell) => cell.iUnit == null);
                break;
            case ESKillRangeType.Circle:
                currentUnitAttackableCells = hexGrid.GetCellsInRange(fromCell, skill.AimDistance, (cell) => !cell.IsUnderwater, true);
                markCells(currentUnitAttackableCells, Color.red, new Color(1f, 0, 0, 0.4f),
                    (cell) =>
                    {
                        var hittables = hexGrid.GetCellsInRange(cursorCell, skill.HitRange);
                        return hittables.Contains(cell);
                    },
                    (cell) => cell.iUnit == null);
                break;
            case ESKillRangeType.Line:
                currentUnitAttackableCells.Clear();
                var direct = (cursorPosition - fromCell.Position).normalized;
                
                break;
        }
    }

    // 選擇腳色
    public void SelectUnit(IHexUnit unit)
    {
        selectedUnit = unit;
        if (selectedUnit != mChessMgr.CurrentUnit)
        {
            attackAim = false;
            markCells(new List<HexCell>() { selectedUnit.Location }, new Color(1, 0, 0, 0.4f));
        }
        mChessMgr.FocusOnUnit(unit, selectedUnit == mChessMgr.CurrentUnit);
    }

    public void OnSelectSkill(ISkill skill)
    {
        SelectUnit(mChessMgr.CurrentUnit);
        SearchAttackableCells(mChessMgr.CurrentUnit, skill.AimDistance);
        attackAim = true;
        selectedSkill = skill;
    }

    public void CancelSelect()
    {
        if (selectedUnit != null)
        {
            selectedUnit.Location.DisableHightlight();
            selectedUnit = null;
        }
        foreach (var c in currentUnitReachableCells)
        {
            c.DisableHightlight();
        }
        hexGrid.ClearPath();
        attackAim = false;
    }

    public void CancelAim()
    {   
        attackAim = false;
        foreach (var cell in currentUnitAttackableCells)
        {
            cell.DisableHightlight();
        }
        currentUnitAttackableCells.Clear();
    }

    public void SearchAttackableCells(IHexUnit unit, int aimDistance)
    {
        currentUnitAttackableCells = hexGrid.GetCellsInRange(unit.Location, aimDistance);
    }

    #region -- Mark/Unmark cell methods --
    private List<HexCell> markedCells = new List<HexCell>();

    private void markCells(IList<HexCell> cells, Color color)
    {
        unmarkCells();
        markedCells.AddRange(cells);
        foreach (HexCell cell in cells)
        {
            cell.EnableHighlight(color);
        }
    }

    private void markCells(
        IList<HexCell> cells, Color mainColor, Color subColor,
        Func<HexCell, bool> checkMain, Func<HexCell, bool> checkSub)
    {
        if (cells == null)
            return;
        unmarkCells();
        markedCells.AddRange(cells);
        foreach (HexCell cell in cells)
        {
            if (checkMain(cell))
                cell.EnableHighlight(mainColor);
            else if (checkSub(cell))
                cell.EnableHighlight(subColor);
        }
    }

    private void unmarkCells()
    {
        if (markedCells != null)
        {
            foreach (HexCell cell in markedCells)
                cell.DisableHightlight();
            markedCells.Clear();
        }
    }

    // 標記可移動的所有地圖格
    private void markReachableCells(HexCell cell)
    {
        markCells(currentUnitReachableCells, Color.blue, new Color(0, 0, 1f, 0.4f),
            (c) => c == cell && c.iUnit == null, (c) => c.iUnit == null);
        attackAim = false;
    }
    #endregion

    #region -- IOperationHandler methods --
    public byte OperationCode => (byte)EOperationCode.GameLogic;

    public void Handle(IPeer peer, IDictionary<byte, object> payload)
    {
        Debug.Log($"Receive payload in handler[{OperationCode}].");
        foreach(var pair in payload)
        {
            switch (pair.Key)
            {
                case (byte)EGameSwitchCode.MoveCommand:
                    onMoveCommand((MoveCommand)pair.Value);
                    break;
                case (byte)EGameSwitchCode.AttackCommand:
                    onAttackCommand((AttackCommand)pair.Value);
                    break;
            }
        }
    }

    public void UpdateHandler(float deltaTime)
    {
        
    }

    public void OnPeerConnected(IPeer peer)
    {
        Peer = peer;
    }

    public void OnPeerDisconnected(IPeer peer)
    {
        Peer = null;
    }
    #endregion

    #region -- Send packet methods --
    public void RequestMove(int unitId, int cellId)
    {
        Peer.Send(
            OperationCode, 
            new Dictionary<byte, object>
            {
                { (byte)EGameSwitchCode.MoveRequest, new MoveCommand{UnitId = unitId, CellId = cellId } }
            },
            EReliability.ReliableOrdered);
        Debug.Log("Send request move packet");
    }

    public void RequestAttack(int attacker, int defender)
    {
        Peer.Send(
            OperationCode,
            new Dictionary<byte, object>
            {
                { (byte)EGameSwitchCode.AttackRequest, new AttackCommand{AttackerId = attacker, DefenderId = defender } }
            },
            EReliability.ReliableOrdered);
        Debug.Log("Send request attack packet");
    }
    #endregion

    #region -- Events of packets --
    private void onMoveCommand(MoveCommand moveCmd)
    {
        Debug.Log("Receive move command");
        mChessMgr.DoMove(moveCmd.UnitId, moveCmd.CellId);
    }

    private void onAttackCommand(AttackCommand atkCmd)
    {
        Debug.Log("Receive attack command");
        mChessMgr.DoAttack(atkCmd.AttackerId, atkCmd.DefenderId, atkCmd.SkillInfo);
    }
    #endregion
}

public interface IPlayer
{
    int Group { get; }
    void AwakeWithUnit(IHexUnit unit);
    void OnSelectSkill(ISkill skill);
}