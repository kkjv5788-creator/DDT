using UnityEngine;
using TMPro;

/// <summary>
/// 튜토리얼 대사 전용 UI 컨트롤러
/// 기존 TutorialUIController와 독립적으로 작동
/// </summary>
public class TutorialDialogueUIController : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas dialogueCanvas; // World Space Canvas (대사 전용)

    [Header("UI Elements")]
    public TextMeshProUGUI dialogueText;  // 대사 텍스트

    [Header("Colors")]
    public Color dialogueColor = Color.white;

    [Header("Animation")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.3f;

    CanvasGroup _canvasGroup;
    float _fadeTimer;
    bool _isFading;
    bool _targetVisible;

    void Awake()
    {
        // CanvasGroup 추가 (페이드 인/아웃용)
        if (dialogueCanvas != null)
        {
            if (!_canvasGroup)
            {
                _canvasGroup = dialogueCanvas.gameObject.GetComponent<CanvasGroup>();
                if (!_canvasGroup)
                {
                    _canvasGroup = dialogueCanvas.gameObject.AddComponent<CanvasGroup>();
                }
            }

            // 초기 상태: 숨김
            _canvasGroup.alpha = 0f;
            dialogueCanvas.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 페이드 처리
        HandleFade();
    }

    void HandleFade()
    {
        if (!_isFading) return;

        _fadeTimer += Time.deltaTime;

        if (_targetVisible)
        {
            // Fade In
            float t = Mathf.Clamp01(_fadeTimer / fadeInDuration);
            _canvasGroup.alpha = t;

            if (t >= 1f)
            {
                _isFading = false;
            }
        }
        else
        {
            // Fade Out
            float t = Mathf.Clamp01(_fadeTimer / fadeOutDuration);
            _canvasGroup.alpha = 1f - t;

            if (t >= 1f)
            {
                _isFading = false;
                dialogueCanvas.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 대사 표시
    /// </summary>
    public void ShowDialogue(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
            dialogueText.color = dialogueColor;
        }

        FadeIn();
    }

    /// <summary>
    /// 페이드 인
    /// </summary>
    public void FadeIn()
    {
        if (dialogueCanvas == null || _canvasGroup == null) return;

        if (!dialogueCanvas.gameObject.activeSelf)
        {
            dialogueCanvas.gameObject.SetActive(true);
        }

        _targetVisible = true;
        _isFading = true;
        _fadeTimer = 0f;
    }

    /// <summary>
    /// 페이드 아웃
    /// </summary>
    public void FadeOut()
    {
        if (_canvasGroup == null) return;

        _targetVisible = false;
        _isFading = true;
        _fadeTimer = 0f;
    }

    /// <summary>
    /// UI 숨김
    /// </summary>
    public void Hide()
    {
        FadeOut();
    }
}
