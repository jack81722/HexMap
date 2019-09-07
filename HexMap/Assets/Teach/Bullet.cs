using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 子彈
public class Bullet : MonoBehaviour
{   
    public float speed;

    // 子彈存活時間
    private float lifeTime;

    // Start is called before the first frame update
    void Start()
    {
        // 初始化
        lifeTime = 2f;
    }

    // Update is called once per frame
    void Update()
    {
        // 存活時間減掉每個frame的時間
        lifeTime -= Time.deltaTime;

        // 取得座標
        Vector3 pos = transform.position;

        // X軸座標 = 速度 * 單位時間
        pos.x += speed * Time.deltaTime;

        // 把修改的座標，給Unity的現有座標
        transform.position = pos;

        if (lifeTime <= 0)
            Destroy(gameObject);        // 刪除該遊戲物件
    }
}
