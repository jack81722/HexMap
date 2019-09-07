using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // 腳色本身的能力值
    public string name;
    public float speed;

    // 開火速度
    public float fireRate = 0.1f;
    private float fireRateTimer;

    public Bullet bulletPrefab;

    // Start is called before the first frame update
    void Start()
    {
        // 初始化
        fireRateTimer = 0.1f;
    }

    // Update is called once per frame
    void Update()
    {
        fireRateTimer += Time.deltaTime;
        // 按下空白鍵時
        if (Input.GetKey(KeyCode.Space))
        {   
            if (fireRateTimer >= fireRate)
            {
                // 發射子彈
                Instantiate(bulletPrefab, transform.position, Quaternion.identity);
                fireRateTimer %= fireRate;
            }
        }

        //      w
        //    a s d
        float x = Input.GetAxis("Horizontal");          // a - d        ←  →
        float z = Input.GetAxis("Vertical");            // w - s        ↑  ↓

        // 取得座標
        Vector3 pos = transform.position;

        pos += new Vector3(x, 0, z) * speed * Time.deltaTime;

        // 把修改的座標，給Unity的現有座標
        transform.position = pos;
    }
}
