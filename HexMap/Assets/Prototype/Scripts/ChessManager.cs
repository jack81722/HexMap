using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChessManager : MonoBehaviour
{
    public HexGrid hexGrid;
    public Chess chessPrefab;
    public CinemachineBrain cameraBrain;


    // 被選擇的棋子
    private Chess selectedChess;
    private bool moving;
    private List<HexCell> path;

    private void Start()
    {
        var chess = CreateUnit(hexGrid.GetCell(new HexCoordinate(hexGrid.cellCountX / 2, hexGrid.cellCountZ / 2)));
        cameraBrain.ActiveVirtualCamera.LookAt = chess.transform;
        cameraBrain.ActiveVirtualCamera.Follow = chess.transform;
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
        // 右鍵
        if (Input.GetMouseButtonDown(1))
        {
            selectedChess = null;
            // 如果取消選擇，關閉顯示路徑
            hexGrid.ClearPath();
        }

        // 當點擊滑鼠後(左鍵)
        if (Input.GetMouseButtonDown(0))
        {
            // 偵測射線是否打到棋子
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit info))
            {   
                // 從上一層物件，取得棋子腳本
                Chess hitChess = info.collider.GetComponentInParent<Chess>();
                // 如果取得的腳本非空值，則紀錄
                if (hitChess != null)
                {
                    selectedChess = hitChess;
                    //Debug.Log("Hit Chess");
                }
            }
        }

        // 如果有選擇的棋子
        if (selectedChess != null)
        {
            if(Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit info))
            {
                // 從上一層物件，取得棋子腳本
                Chess hitChess = info.collider.GetComponentInParent<Chess>();
                // 如果取得的腳本為空值，則表示偵測到地板
                if (hitChess == null && !moving)
                {
                    HexCell cell = hexGrid.GetCell(info.point);
                    hexGrid.FindPath(selectedChess.Location, cell, selectedChess.speed);
                    
                    if (Input.GetMouseButtonDown(0))
                    {
                        moving = true;
                        // 把搜尋到的路徑存起來
                        path = hexGrid.GetPath();
                        StartCoroutine(Moving(path));
                    }
                }
            }
        }
    }

    
    private IEnumerator Moving(List<HexCell> path)
    {
        int p = 1;
        HexCell next;
        Vector3 pos = selectedChess.transform.position;
        float timer = 0;
        while (p < path.Count)
        {
            next = path[p];
            timer += Time.deltaTime;
            selectedChess.transform.position = Vector3.Lerp(pos, next.Position, timer / 1f);
            yield return null;
            if (timer >= 1f)
            {
                selectedChess.Location = next;
                pos = next.Position;
                next = path[p];
                timer = 0;
                p++;
            }
            
        }
        moving = false;
        selectedChess = null;
        hexGrid.ClearPath();
        path.Clear();
    }
}
