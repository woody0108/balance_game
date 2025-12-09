using UnityEngine;

public class CameraScaler : MonoBehaviour
{
    public float targetAspect = 1080f / 1920f; // 9:16

    void Start()
    {
        Camera cam = Camera.main;
        float windowAspect = (float)Screen.width / Screen.height;

        if (windowAspect > targetAspect)
        {
            // 가로가 더 넓은 경우 → 세로 기준 맞춤
            cam.orthographicSize = 5f;
        }
        else
        {
            // 세로가 더 긴 경우 → 화면 꽉 채우기 위해 size 조절
            float scale = targetAspect / windowAspect;
            cam.orthographicSize = 5f * scale;
        }
    }
}
