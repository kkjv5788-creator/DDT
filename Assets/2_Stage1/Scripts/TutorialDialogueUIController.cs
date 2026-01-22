using UnityEngine;
using TMPro;

public class TutorialDialogueUIController : MonoBehaviour
{
    [Header("UI Root")]
    // [수정됨] Canvas 대신 GameObject로 변경하여 패널을 직접 넣을 수 있게 함
    public GameObject dialoguePanel; 

    [Header("UI Elements")]
    public TextMeshProUGUI dialogueText;  // 대사 텍스트
    public TextMeshProUGUI hintText;      // 힌트 텍스트

    [Header("Colors")]
    public Color dialogueColor = Color.white;
    public Color hintColor = Color.yellow;

    [Header("Animation")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.3f;

    CanvasGroup _canvasGroup;
    float _fadeTimer;
    bool _isFading;
    bool _targetVisible;

    void Awake()
    {
        // dialoguePanel이 연결되어 있는지 확인
        if (dialoguePanel != null)
        {
            // CanvasGroup 컴포넌트 가져오기 (없으면 자동 추가)
            _canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
            if (!_canvasGroup)
            {
                _canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
            }

            // 초기 상태: 투명하게 만들고 비활성화
            _canvasGroup.alpha = 0f;
            dialoguePanel.SetActive(false);
        }
        else
        {
            Debug.LogError("TutorialDialogueUIController: 대화창 패널(Dialogue Panel)이 연결되지 않았습니다!");
        }
    }

    void Update()
    {
        HandleFade();
    }

    void HandleFade()
    {
        if (!_isFading || dialoguePanel == null) return;

        _fadeTimer += Time.deltaTime;

        if (_targetVisible)
        {
            // 페이드 인 (점점 나타남)
            float t = Mathf.Clamp01(_fadeTimer / fadeInDuration);
            _canvasGroup.alpha = t;

            if (t >= 1f) _isFading = false;
        }
        else
        {
            // 페이드 아웃 (점점 사라짐)
            float t = Mathf.Clamp01(_fadeTimer / fadeOutDuration);
            _canvasGroup.alpha = 1f - t;

            if (t >= 1f)
            {
                _isFading = false;
                dialoguePanel.SetActive(false); // 다 사라지면 끄기
            }
        }
    }

    public void ShowDialogue(string text, string hint = "")
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
            dialogueText.color = dialogueColor;
        }

        if (hintText != null)
        {
            if (!string.IsNullOrEmpty(hint))
            {
                hintText.text = hint;
                hintText.color = hintColor;
                hintText.gameObject.SetActive(true);
            }
            else
            {
                hintText.gameObject.SetActive(false);
            }
        }

        FadeIn();
    }

    public void FadeIn()
    {
        if (dialoguePanel == null || _canvasGroup == null) return;

        if (!dialoguePanel.activeSelf)
        {
            dialoguePanel.SetActive(true);
        }

        _targetVisible = true;
        _isFading = true;
        _fadeTimer = 0f;
    }

    public void FadeOut()
    {
        if (_canvasGroup == null) return;

        _targetVisible = false;
        _isFading = true;
        _fadeTimer = 0f;
    }

    public void Hide()
    {
        if (hintText != null) hintText.gameObject.SetActive(false);
        FadeOut();
    }
}