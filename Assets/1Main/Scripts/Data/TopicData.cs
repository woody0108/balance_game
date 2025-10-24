using System;
using UnityEngine;

/// <summary>
/// 주제 데이터 클래스
/// Firestore 문서와 매핑
/// </summary>
[Serializable]
public class TopicData
{
    public string topicId;
    public string question;
    public string optionA;
    public string optionB;
    public int votesA;
    public int votesB;
    
    [NonSerialized]
    public int totalVotes;

    // 퍼센트 계산 (0~100)
    public float PercentageA
    {
        get
        {
            if (totalVotes == 0) return 50f;
            return (votesA / (float)totalVotes) * 100f;
        }
    }

    public float PercentageB
    {
        get
        {
            if (totalVotes == 0) return 50f;
            return (votesB / (float)totalVotes) * 100f;
        }
    }

    // 비율 (0~1, Layout Element용)
    public float RatioA
    {
        get
        {
            if (totalVotes == 0) return 0.5f;
            return votesA / (float)totalVotes;
        }
    }

    public float RatioB
    {
        get
        {
            if (totalVotes == 0) return 0.5f;
            return votesB / (float)totalVotes;
        }
    }

    public TopicData()
    {
        topicId = "";
        question = "";
        optionA = "";
        optionB = "";
        votesA = 0;
        votesB = 0;
        totalVotes = 0;
    }

    /// <summary>
    /// 총 투표수 계산
    /// </summary>
    public void CalculateTotalVotes()
    {
        totalVotes = votesA + votesB;
    }

    public override string ToString()
    {
        return $"[Topic] {question}\n  A: {optionA} ({PercentageA:F1}%)\n  B: {optionB} ({PercentageB:F1}%)";
    }
}