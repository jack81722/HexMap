using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HeadQueue : MonoBehaviour
{
    RectTransform rect;
    public float width, height, padding;
    public Vector2 offset;
    public GameObject headPrefab;
    public List<RectTransform> heads;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        width = rect.rect.width;
        height = rect.rect.height;
        heads = new List<RectTransform>();
    }

    public void RefreshQueue(IList<IHexUnit> queue, UnityAction<IHexUnit> buttonAction)
    {
        if (queue.Count != heads.Count)
            Resize(queue.Count);
        for (int i = 0; i < heads.Count; i++)
        {
            IHexUnit unit = queue[i];
            heads[i].GetComponentInChildren<TextMeshProUGUI>().text = unit.name/*.Split(' ')[1]*/;
            Button botton = heads[i].GetComponent<Button>();
            if (unit.IsAlive)
            {
                botton.interactable = true;
                botton.onClick.RemoveAllListeners();
                botton.onClick.AddListener(() => buttonAction?.Invoke(unit));
            }
            else
            {
                // 把按鈕的可控性關閉
                botton.interactable = false;
            }
        }
    }

    public void Resize(int headCount)
    {   
        if (heads.Count < headCount)
        {
            int count = headCount - heads.Count;
            for (int i = 0; i < count; i++)
            {
                var head = Instantiate(headPrefab, transform);
                heads.Add(head.GetComponent<RectTransform>());
            }
        }
        else if (heads.Count > headCount)
        {
            int count = (heads.Count - headCount);
            foreach (var removed in heads.GetRange(count - 1 - count, count))
            {
                Destroy(removed.gameObject);
            }
            heads.RemoveRange(count - 1 - count, count);
        }
        float headWidth = headPrefab.GetComponent<RectTransform>().rect.width;
        float totalWidth = (heads.Count - 1) * padding + heads.Count * headWidth;
        for(int i = 0; i < heads.Count; i++)
        {   
            heads[i].position = new Vector2(rect.position.x - totalWidth / 2 + headWidth / 2 + headWidth * i + padding * i, rect.position.y + height * -rect.pivot.y / 2);
        }
    }
}
