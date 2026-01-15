using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 앱 전체 흐름(씬 전환/페이드/로비 텔레포트/테스트용 높이 전환)을 단일 책임으로 관리.
/// - 씬 로드는 Build Index만 사용.
/// - OVRScreenFade는 인스펙터로 고정 참조(권장). Camera.main 의존 금지.
/// </summary>
public class AppSceneFlow : MonoBehaviour
{
    [Header("Scene Build Indices (Build Settings order)")]
    [Tooltip("메인(타이틀/로비) 씬 인덱스. 일반적으로 0")]
    public int mainBuildIndex = 0;

    [Tooltip("Stage1 씬 인덱스. 일반적으로 1")]
    public int stageBuildIndex = 1;

    [Header("Fade")]
    [Tooltip("CenterEyeAnchor에 붙은 OVRScreenFade를 연결(권장).")]
    public OVRScreenFade fader;

    [Tooltip("FadeOut 후 대기 시간(초). OVRScreenFade fadeTime과 동일하게 두는 것을 권장.")]
    public float fadeWaitSeconds = 1.0f;

    [Header("Lobby Teleport (optional)")]
    [Tooltip("XR_PlayerRig 루트 Transform(또는 OVRCameraRig 루트). 로비 이동 시 이 Transform을 텔레포트.")]
    public Transform playerRigRoot;

    [Tooltip("로비에서 설 플레이어 기준 위치(옵션).")]
    public Transform lobbyPoint;

    [Header("Editor/Simulator Height Offset (optional)")]
    [Tooltip("옵션 A: 에디터/시뮬레이터에서만 의미 있는 높이 전환 매니저. 빌드에서는 무효로 만들 예정.")]
    public SmartHeightManager heightManager;

    [Header("Title Start (optional)")]
    public Transform titlePoint;
    public bool moveToTitleOnStart = true;

    [Header("Ray Mode Toggle")]
    [Tooltip("3D Physics Ray(스테이지/로비에서 사용). 타이틀에선 OFF")]
    public VRRayInteractor leftRay;

    [Tooltip("3D Physics Ray(스테이지/로비에서 사용). 타이틀에선 OFF")]
    public VRRayInteractor rightRay;

    [Tooltip("왼손 타이틀 UI 레이저. 타이틀에선 ON")]
    public UIRayLaserFeedback leftUILaser;

    [Tooltip("오른손 타이틀 UI 레이저. 타이틀에선 ON")]
    public UIRayLaserFeedback rightUILaser;

    private bool _busy;

    private void Awake()
    {
        // 인스펙터 연결이 최우선. 누락 시 안전장치로 1회 탐색.
        if (fader == null)
            fader = FindObjectOfType<OVRScreenFade>(true);
    }

    private void Start()
    {
        if (moveToTitleOnStart && playerRigRoot != null && titlePoint != null)
        {
            playerRigRoot.SetPositionAndRotation(titlePoint.position, titlePoint.rotation);
            SetTitleMode(true);
        }
        else
        {
            // 타이틀로 시작하지 않는 경우는 기본 게임 모드가 안전
            SetTitleMode(false);
        }
    }

    // ===== Public API =====

    /// <summary>로비(메인 씬에서) 로비 위치로 텔레포트 + 페이드</summary>
    public void GoToLobby()
    {
        if (_busy) return;
        StartCoroutine(Co_GoToLobby());
    }

    /// <summary>스테이지 씬으로 이동(페이드 후 BuildIndex 로드)</summary>
    public void GoToStage()
    {
        if (_busy) return;
        StartCoroutine(Co_LoadScene(stageBuildIndex));
    }

    /// <summary>메인 씬으로 복귀(페이드 후 BuildIndex 로드)</summary>
    public void GoToMain()
    {
        if (_busy) return;
        StartCoroutine(Co_LoadScene(mainBuildIndex));
    }

    /// <summary>
    /// 범용 씬 로드(빌드 인덱스). 외부에서 임의 씬 이동이 필요하면 사용.
    /// </summary>
    public void LoadSceneByIndex(int buildIndex)
    {
        if (_busy) return;
        StartCoroutine(Co_LoadScene(buildIndex));
    }

    // ===== Coroutines =====

    private IEnumerator Co_GoToLobby()
    {
        _busy = true;

        FadeOut();
        yield return new WaitForSeconds(fadeWaitSeconds);

        // 로비 포인트로 텔레포트 (옵션)
        if (playerRigRoot != null && lobbyPoint != null)
        {
            playerRigRoot.SetPositionAndRotation(lobbyPoint.position, lobbyPoint.rotation);
        }

        // 옵션 A: 에디터에서만 동작하도록 SmartHeightManager를 수정할 것
        if (heightManager != null)
        {
            // 메서드명은 유지해라(호환용).
            heightManager.SwitchToGameHeight();
        }

        // 로비/게임 모드: 3D Ray ON, UI Hover OFF
        SetTitleMode(false);

        FadeIn();
        _busy = false;
    }

    private IEnumerator Co_LoadScene(int buildIndex)
    {
        _busy = true;

        // 씬 전환 직전: 타이틀 UI Hover는 무조건 OFF(다음 씬에서 다시 세팅)
        SetTitleMode(false);

        FadeOut();
        yield return new WaitForSeconds(fadeWaitSeconds);

        SceneManager.LoadScene(buildIndex);
        _busy = false;
    }

    private void FadeOut()
    {
        if (fader != null) fader.FadeOut();
    }

    private void FadeIn()
    {
        if (fader != null) fader.FadeIn();
    }

    // ===== Mode Toggle =====

    /// <summary>
    /// true(타이틀): UI Hover ON, 3D Ray OFF
    /// false(로비/스테이지): UI Hover OFF, 3D Ray ON
    /// </summary>
    private void SetTitleMode(bool isTitle)
    {
        if (leftRay != null) leftRay.enabled = !isTitle;
        if (rightRay != null) rightRay.enabled = !isTitle;

        if (leftUILaser != null) leftUILaser.enabled = isTitle;
        if (rightUILaser != null) rightUILaser.enabled = isTitle;
    }

    // ===== Game Quit =====

    /// <summary>
    /// 게임을 완전히 종료합니다. (빌드된 게임에서는 창이 닫히고, 에디터에서는 플레이 모드가 멈춥니다.)
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
            // 에디터에서 플레이 중일 때는 플레이 모드를 중지
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // 실제 빌드된 앱에서는 어플리케이션 종료
            Application.Quit();
        #endif
    }
}
