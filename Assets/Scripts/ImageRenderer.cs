using UnityEngine;
using UnityEngine.UI;

public class ImageRenderer : MonoBehaviour
{
    [SerializeField] private ImageWebSocketClient imageWebSocketClient;
    [SerializeField] private RawImage displayImage;

    private Texture2D texture;

    void Start()
    {
        if (displayImage == null)
        {
            displayImage = GetComponent<RawImage>();
            if (displayImage == null)
            {
                Debug.LogError("[ImageRenderer] RawImage component not found on this GameObject.");
                return;
            }
        }

        if (imageWebSocketClient == null)
        {
            imageWebSocketClient = Object.FindFirstObjectByType<ImageWebSocketClient>();
            if (imageWebSocketClient == null)
            {
                Debug.LogError("[ImageRenderer] ImageWebSocketClient not found in the scene.");
                return;
            }
        }

        texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        displayImage.texture = texture;
        imageWebSocketClient.OnImageReceived += UpdateImage;

        RectTransform rectTransform = displayImage.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(640, 360); // 기본 크기 설정
        }
    }

    private void UpdateImage(byte[] imageData)
    {
        if (texture.LoadImage(imageData))
        {
            texture.Apply();
            displayImage.texture = texture; // 텍스처 재할당
            RectTransform rectTransform = displayImage.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(texture.width, texture.height); // 텍스처 크기에 맞게 조정
            }
            Debug.Log($"[ImageRenderer] JPEG image updated, size: {texture.width}x{texture.height}, RawImage texture assigned: {(displayImage.texture != null)}");
        }
        else
        {
            Debug.LogError("[ImageRenderer] Failed to load JPEG image data.");
        }

        if (!displayImage.enabled)
        {
            Debug.LogWarning("[ImageRenderer] RawImage is disabled. Enabling it now.");
            displayImage.enabled = true;
        }
    }

    void OnDestroy()
    {
        if (imageWebSocketClient != null)
        {
            imageWebSocketClient.OnImageReceived -= UpdateImage;
        }
    }
}