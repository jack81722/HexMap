using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChessManager : MonoBehaviour
{
    public HexGrid hexGrid;
    public Chess chessPrefab;
    public Material Enemy_Material;
    public CinemachineBrain cameraBrain;

    // 被選擇的棋子
    private IHexUnit selectedUnit;
    private HexCell currentCell;
    private Coroutine movingCoroutine;
    private List<HexCell> path;

    private List<IHexUnit> unitQueue;
    private IHexUnit currentUnit;
    private List<HexCell> currentUnitReachableCells;        // 可移動到的所有地圖格

    public RectTransform[] heads;

    // 瞄準狀態
    public bool attackAim;
    public List<HexCell> currentUnitAttackableCells;

    private void Start()
    {
        unitQueue = new List<IHexUnit>();
        currentUnitReachableCells = new List<HexCell>();
        currentUnitAttackableCells = new List<HexCell>();

        List<HexCell> allCells = new List<HexCell>(hexGrid.GetAllValidCells());
        Shuffle(allCells);
        for (int i = 0; i < 10; i++)
        {
            var chess = CreateUnit(allCells[i]);
            chess.name = "Chess " + (char)('A' + i);
            chess.Agility = i * 0.1f + 1;
            if (i > 4)
            {
                chess.Group = 1;
                chess.GetComponentInChildren<MeshRenderer>().material = Enemy_Material;
            }
            unitQueue.Add(chess);
        }
        
        refreshActionQueue();
    }

    public Chess CreateUnit(HexCell cell)
    {   
        var chess = Instantiate(chessPrefab);
        
        chess.transform.SetParent(transform);
        chess.Grid = hexGrid;
        chess.Location = cell;
        
        return chess;
    }

    private void Update()
    {   
        HexCell cell = hexGrid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));  // 先找到指標只到的地圖格
        if (Input.GetMouseButtonDown(0))
        {   
            if (cell != null && cell.iUnit != null)
            {
                // select
                selectedUnit = cell.iUnit;
            }
            else
            {
                // clear
                //skipMoving();
                cancelSelect();
            }
        }
        else if (Input.GetMouseButtonDown(1))
        {
            // 點擊右鍵

            if (attackAim)
            {
                if (cell.iUnit != null &&                       // 如果選擇的格子上有棋子 且
                    cell.iUnit.Group != currentUnit.Group &&    // 如果該格的棋子與當前行動的棋子隊伍(group)不同時
                    currentUnitAttackableCells.Contains(cell))  // 確認該格子是否在可攻擊的格子群(array)中
                {
                    // 進行攻擊
                    Debug.Log("Attack");
                    currentUnit.ActionPoint = 0;
                    refreshActionQueue();
                }
            }
            else
            {
                if (selectedUnit == currentUnit && 
                    currentUnitReachableCells.Contains(cell) && cell.iUnit == null)
                {
                    // 執行移動指令
                    int moveCost = Move(selectedUnit, cell, (cost) =>
                    {
                    // 扣除移動的行動點數
                    if (cost >= 0)
                            selectedUnit.ActionPoint -= cost;
                        else
                        {
                        // do something ...
                    }
                    // 判斷是否有剩餘的行動點數
                    if (selectedUnit.ActionPoint <= 0)
                        {
                        // 耗完行動點數則重新整理行動排序
                        refreshActionQueue();
                        }
                    });

                }
            }
        }
        else
        {
            if(selectedUnit == currentUnit && !attackAim)
            {
                markReachableCells(cell);
            }
        }
    }

    private void cancelSelect()
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

    //private void skipMoving()
    //{   
    //    if (movingCoroutine != null)
    //    {
    //        StopCoroutine(movingCoroutine);
    //        HexCell goal = path[path.Count - 1];
    //        selectedUnit.Location = goal;
    //        selectedUnit = null;
    //        hexGrid.ClearPath();
    //        movingCoroutine = null;
    //        refreshActionQueue();
    //    }
    //}

    // 標記可移動的所有地圖格
    private void markReachableCells(HexCell cell)
    {
        foreach (var c in currentUnitReachableCells)
        {
            if (c == cell && c.iUnit == null)
                cell.EnableHighlight(Color.blue);
            else if (c.iUnit == null)
                c.EnableHighlight(new Color(0, 0, 1f, 0.4f));
        }
        attackAim = false;
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
    }

    private void refreshActionQueue()
    {   
        unitQueue.Sort((unitA, unitB) =>
        {
            return remain(unitA) - remain(unitB);
        });
        refreshHeadQueue(unitQueue);
        setChessAction(unitQueue[0]);
        float time = remainTime(currentUnit);
        for(int i = 0; i < unitQueue.Count; i++)
        {
            IHexUnit unit = unitQueue[i];
            unit.ActionProcess = (unit.ActionProcess + time * unit.Agility);
        }
        currentUnit.ActionProcess %= 100;

        // remain time of next action
        int remain(IHexUnit unit)
        {
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
        foreach(var c in currentUnitReachableCells)
        {
            c.DisableHightlight();
        }
        currentUnit = unitQueue[0];
        currentUnit.ActionPoint = 20;       // 給予角色行動點數
        cameraBrain.ActiveVirtualCamera.LookAt = currentUnit.transform;
        cameraBrain.ActiveVirtualCamera.Follow = currentUnit.transform;
        // 取得可移動的範圍
        currentUnitReachableCells = hexGrid.GetReachableCells(currentUnit.Location,currentUnit.Speed);
    }

    private void refreshHeadQueue(IList<IHexUnit> queue)
    {
        for(int i = 0; i < heads.Length; i++)
        {
            Chess unit = (Chess)queue[i];
            heads[i].GetComponentInChildren<TextMeshProUGUI>().text = unit.name.Split(' ')[1];
            Button botton = heads[i].GetComponent<Button>();
            botton.onClick.RemoveAllListeners();
            botton.onClick.AddListener(() => 
            {
                SelectUnit(unit);
                if(unit == currentUnit)
                    markReachableCells(unit.Location);
            });
        }
    }

    public void SelectUnit(IHexUnit unit)
    {   
        selectedUnit = unit;
        if(selectedUnit != currentUnit)
            selectedUnit.Location.EnableHighlight(new Color(1, 0, 0, 0.4f));
        cameraBrain.ActiveVirtualCamera.LookAt = unit.transform;
        cameraBrain.ActiveVirtualCamera.Follow = unit.transform;
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

    // 搜尋可攻擊的範圍
    private void SearchAttackableCells(IHexUnit unit, int atkRange)
    {
        currentUnitAttackableCells = hexGrid.GetAttackableCells(unit.Location, atkRange);
        foreach(HexCell cell in currentUnitAttackableCells)   // atkCells[0], [1], [2]
        {
            if (cell.iUnit == null)
                cell.EnableHighlight(new Color(1f, 0, 0, 0.4f));
            else if(cell.iUnit != currentUnit && cell.iUnit.Group != currentUnit.Group)
                cell.EnableHighlight(Color.red);
        }
    }

    public void OnClickAttackButton(int atkRange)
    {
        //Debug.Log($"Unit:{((Chess)selectedUnit).name}, range:{atkRange}");
        // 選擇的單位與可行動單位是同一個
        SelectUnit(currentUnit);
        SearchAttackableCells(currentUnit, atkRange);
        attackAim = true;
    }
}
