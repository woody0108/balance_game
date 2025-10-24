using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 주제 카드 UI 컴포넌트
/// - Inspector에서 모든 UI 요소 연결
/// - TopicData를 받아서 UI 업데이트만 담당
/// - 로직 없음, 순수 View
/// </summary>
public class TopicCardUI : MonoBehaviour
{
    [Header("=== Title ===")]
    [SerializeField] private TextMeshProUGUI questionText;

    [Header("=== Vote Buttons ===")]
    [SerializeField] private Button buttonA;
    [SerializeField] private Button buttonB;
    [SerializeField] private TextMeshProUGUI buttonAText;
    [SerializeField] private TextMeshProUGUI buttonBText;

    [Header("=== Result Bar ===")]
    [SerializeField] private TextMeshProUGUI labelA;
    [SerializeField] private TextMeshProUGUI labelB;
    [SerializeField] private GameObject fillA; // LayoutElement가 있는 오브젝트
    [SerializeField] private GameObject fillB;
    [SerializeField] private TextMeshProUGUI percentTextA;
    [SerializeField] private TextMeshProUGUI percentTextB;

    [Header("=== Animation Settings ===")]
    [SerializeField] private float animationDuration = 0.5f;

    // 현재 표시 중인 데이터
    private TopicData currentData;

    // LayoutElement 캐싱
    private LayoutElement layoutElementA;
    private LayoutElement layoutElementB;

    private void Awake()
    {
        // LayoutElement 캐싱
        if (fillA != null)
            layoutElementA = fillA.GetComponent<LayoutElement>();
        
        if (fillB != null)
            layoutElementB = fillB.GetComponent<LayoutElement>();

        // 버튼 이벤트 연결은 UIManager가 함
        // 여기서는 안 함!
    }

    /// <summary>
    /// 버튼 클릭 이벤트 등록 (외부에서 호출)
    /// </summary>
    public void SetButtonListeners(System.Action onVoteA, System.Action onVoteB)
    {
        if (buttonA != null)
        {
            buttonA.onClick.RemoveAllListeners();
            buttonA.onClick.AddListener(() => onVoteA?.Invoke());
        }

        if (buttonB != null)
        {
            buttonB.onClick.RemoveAllListeners();
            buttonB.onClick.AddListener(() => onVoteB?.Invoke());
        }

        Debug.Log("[TopicCardUI] 버튼 리스너 등록 완료");
    }

    /// <summary>
    /// TopicData로 UI 업데이트 (메인 메서드)
    /// </summary>
    public void UpdateUI(TopicData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[TopicCardUI] TopicData가 null입니다");
            return;
        }

        currentData = data;

        // 질문 텍스트
        if (questionText != null)
            questionText.text = data.question;

        // 버튼 텍스트
        if (buttonAText != null)
            buttonAText.text = data.optionA;
        
        if (buttonBText != null)
            buttonBText.text = data.optionB;

        // 라벨 텍스트
        if (labelA != null)
            labelA.text = data.optionA;
        
        if (labelB != null)
            labelB.text = data.optionB;

        // 결과 바 업데이트
        UpdateResultBar(data);

        Debug.Log($"[TopicCardUI] UI 업데이트 완료: {data.question}");
    }

    /// <summary>
    /// 결과 바만 업데이트 (실시간 투표율 변경용)
    /// </summary>
    public void UpdateResultBar(TopicData data, bool animated = false)
    {
        if (data == null) return;

        // 퍼센트 텍스트
        if (percentTextA != null)
            percentTextA.text = $"{data.PercentageA:F1}%";
        
        if (percentTextB != null)
            percentTextB.text = $"{data.PercentageB:F1}%";

        // LayoutElement로 비율 조정
        if (layoutElementA != null)
        {
            if (animated)
            {
                // TODO: 애니메이션 추가
                layoutElementA.flexibleWidth = data.PercentageA;
            }
            else
            {
                layoutElementA.flexibleWidth = data.PercentageA;
            }
        }

        if (layoutElementB != null)
        {
            if (animated)
            {
                // TODO: 애니메이션 추가
                layoutElementB.flexibleWidth = data.PercentageB;
            }
            else
            {
                layoutElementB.flexibleWidth = data.PercentageB;
            }
        }

        Debug.Log($"[TopicCardUI] 결과 바 업데이트: {data.PercentageA:F1}% vs {data.PercentageB:F1}%");
    }

    /// <summary>
    /// 버튼 활성화/비활성화
    /// </summary>
    public void SetButtonsInteractable(bool interactable)
    {
        if (buttonA != null)
            buttonA.interactable = interactable;
        
        if (buttonB != null)
            buttonB.interactable = interactable;

        Debug.Log($"[TopicCardUI] 버튼 상태: {(interactable ? "활성" : "비활성")}");
    }

    #region Debug
    [ContextMenu("Test: Update with Dummy Data")]
    private void TestDummyData()
    {
        var dummyData = new TopicData
        {
            question = "테스트 질문",
            optionA = "옵션A",
            optionB = "옵션B",
            votesA = 60,
            votesB = 40
        };
        dummyData.CalculateTotalVotes();

        UpdateUI(dummyData);
    }
    #endregion
}