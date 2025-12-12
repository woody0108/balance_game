using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Firebase.Firestore;

[System.Serializable]
public class CategoryItem
{
    public string question;
    public string optionA;
    public string optionB;
}

[System.Serializable]
public class CategoryBlock
{
    public string category;
    public List<CategoryItem> items;
}

[System.Serializable]
public class CategoryRoot
{
    public List<CategoryBlock> categories;
}

public class FirestoreAutoUploader : MonoBehaviour
{   public static FirestoreAutoUploader Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
            
    



    public async Task UploadFromJSON()
    {
        // JSON 로드
        
        TextAsset json = Resources.Load<TextAsset>("categories");
        if (json == null)
        {
            Debug.LogError("categories.json 리소스를 찾을 수 없음!");
            return;
        }

        CategoryRoot root = JsonUtility.FromJson<CategoryRoot>(json.text);

        foreach (var category in root.categories)
        {
            string collectionPath = category.category;

            Debug.Log($"== 업로드 시작: {collectionPath} ==");

            for (int i = 0; i < category.items.Count; i++)
            {
                var item = category.items[i];

                string documentId = $"{category.category}-{i + 1}";

                // 🔍 중복 검사: 동일한 question이 있는지 확인
                Query query = FirebaseManager.Instance.db.Collection(collectionPath).WhereEqualTo("question", item.question);
                QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

                if (querySnapshot.Count > 0)
                {
                    Debug.Log($"⚠ 중복 발견 → 건너뜀: {item.question}");
                    continue;
                }

                // Firestore 데이터
                var data = new Dictionary<string, object>
                {
                    { "question", item.question },
                    { "optionA", item.optionA },
                    { "optionB", item.optionB },
                    { "votesA", 0 },
                    { "votesB", 0 }
                };

                await FirebaseManager.Instance.db.Collection(collectionPath).Document(documentId).SetAsync(data);
                Debug.Log($"업로드 완료 → {collectionPath}/{documentId}");
            }
        }

        Debug.Log("🔥 모든 카테고리 업로드 완료!");
    }
}
