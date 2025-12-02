using System.Collections.Generic;
using UnityEngine;

public class UIObjectPoolingManager : MonoBehaviour
{
    public static UIObjectPoolingManager Instance;

    [System.Serializable]
    public class PoolItem
    {
        public string key;                  // 고유 키 (ex: "ColorButton", "Popup", "LoadingUI")
        public GameObject prefab;           // 해당 UI 프리팹
        public int preloadCount = 5;        // 미리 생성할 개수
    }

    [Header("UI Pool List")]
    public List<PoolItem> uiPoolItems = new List<PoolItem>();

    // 내부 풀: key → 큐
    private Dictionary<string, Queue<GameObject>> pools = new Dictionary<string, Queue<GameObject>>();

    // 프리팹 직접 참조용
    private Dictionary<string, GameObject> prefabLookup = new Dictionary<string, GameObject>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 설정된 모든 PoolItem 미리 생성
    /// </summary>
    private void InitializePools()
    {
        foreach (var item in uiPoolItems)
        {
            Queue<GameObject> queue = new Queue<GameObject>();

            // lookup 저장
            prefabLookup[item.key] = item.prefab;

            // 미리 생성
            for (int i = 0; i < item.preloadCount; i++)
            {
                GameObject obj = CreateNewObject(item.key);
                obj.SetActive(false);
                queue.Enqueue(obj);
            }

            pools[item.key] = queue;
        }
    }

    /// <summary>
    /// key로 오브젝트 꺼내기
    /// </summary>
    public GameObject Get(string key, Transform parent = null)
    {
        // 풀 없음 → 자동 생성
        if (!pools.ContainsKey(key))
        {
            pools[key] = new Queue<GameObject>();
            prefabLookup[key] = null;
        }

        // 큐가 비어있으면 Instantiate
        if (pools[key].Count == 0)
        {
            GameObject created = CreateNewObject(key);
            SetParent(created, parent);
            created.SetActive(true);
            return created;
        }

        // 큐에서 가져오기
        GameObject obj = pools[key].Dequeue();
        SetParent(obj, parent);
        obj.SetActive(true);
        return obj;
    }

    /// <summary>
    /// 오브젝트 반환 (비활성화 후 풀로 복귀)
    /// </summary>
    public void Return(string key, GameObject obj)
    {
        obj.SetActive(false);
        pools[key].Enqueue(obj);
    }


    /// <summary>
    /// 프리팹 Instantiate
    /// </summary>
    private GameObject CreateNewObject(string key)
    {
        if (!prefabLookup.ContainsKey(key) || prefabLookup[key] == null)
        {
            Debug.LogError($"[UIPool] '{key}' 프리팹이 설정되지 않음!");
            return null;
        }

        GameObject newObj = Instantiate(prefabLookup[key], transform);
        newObj.name = $"{key}_Pooled";
        return newObj;
    }

    /// <summary>
    /// 부모 자동 설정
    /// </summary>
    private void SetParent(GameObject obj, Transform parent)
    {
        if (parent != null)
            obj.transform.SetParent(parent, false);
        else
            obj.transform.SetParent(transform, false);
    }
}
