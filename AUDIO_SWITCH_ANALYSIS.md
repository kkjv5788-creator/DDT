# AudioSource 클립 변경 구현 분석

## 현재 상황 분석

### 1. 버튼 구조
- **경로**: `Spot_Title/Title__UI_Group/Canvas/play_btn`
- **프리팹**: `Assets/1_Main/Prefab/Spot_Title.prefab`
- **현재 On Click 이벤트**:
  1. `MainMenuManager.GoToLobby()` - 로비로 이동
  2. `ButtonFeedback.PlayFeedback()` - 버튼 피드백 (애니메이션 + 효과음)

### 2. 오디오 파일 위치
- **현재 재생 중**: `Assets/1_Main/BGM/Neosign Dudungtak!.mp3`
- **변경할 파일**: `Assets/1_Main/BGM/Dudungtak! Neo Street-2.mp3`

### 3. AudioSource 위치 추정
- 씬이나 씬 매니저에 별도의 AudioSource가 있을 가능성 높음
- BGM 재생용 AudioSource는 보통 DontDestroyOnLoad나 씬 레벨에 존재

## 필요한 구현 방법

### 방법 1: MainMenuManager에 메서드 추가 (권장)

`MainMenuManager.cs`에 BGM 변경 기능 추가:

```csharp
using UnityEngine;
using System.Collections;

public class MainMenuManager : MonoBehaviour
{
    [Header("연결할 것들")]
    public Transform playerRig;       
    public GameObject titleGroup;     
    public OVRScreenFade screenFader; 
    public SmartHeightManager heightManager;
    
    [Header("BGM 설정")]
    public AudioSource bgmAudioSource; // BGM용 AudioSource 연결
    public AudioClip newBGMClip;       // "Dudungtak! Neo Street-2.mp3" 연결
    
    [Header("설정")]
    public Vector3 lobbyPosition = new Vector3(0, 0, 0);
    
    // BGM 변경 메서드 (버튼 On Click에서 호출)
    public void ChangeBGM()
    {
        if (bgmAudioSource != null && newBGMClip != null)
        {
            bgmAudioSource.clip = newBGMClip;
            bgmAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("BGM AudioSource 또는 새 클립이 설정되지 않았습니다.");
        }
    }
    
    // 기존 코드...
    public void GoToLobby()
    {
        // BGM 변경 후 이동
        ChangeBGM();
        StartCoroutine(TeleportSequence());
    }
    
    // ... 나머지 기존 코드 ...
}
```

**Unity 에디터 설정**:
1. `MainMenuManager` 컴포넌트에 `Bgm Audio Source` 필드에 AudioSource 연결
2. `New BGM Clip` 필드에 "Dudungtak! Neo Street-2.mp3" 연결
3. `play_btn`의 On Click에 `ChangeBGM()` 추가

### 방법 2: 별도 BGM 매니저 스크립트 생성

더 체계적인 관리가 필요한 경우:

```csharp
using UnityEngine;

public class BGMManager : MonoBehaviour
{
    private static BGMManager instance;
    public static BGMManager Instance => instance;
    
    [Header("AudioSource")]
    public AudioSource bgmSource;
    
    [Header("BGM Clips")]
    public AudioClip titleBGM;        // "Neosign Dudungtak!.mp3"
    public AudioClip gameplayBGM;     // "Dudungtak! Neo Street-2.mp3"
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (bgmSource == null)
                bgmSource = GetComponent<AudioSource>();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void ChangeBGM(AudioClip newClip)
    {
        if (bgmSource != null && newClip != null)
        {
            bgmSource.clip = newClip;
            bgmSource.Play();
        }
    }
    
    public void ChangeToGameplayBGM()
    {
        ChangeBGM(gameplayBGM);
    }
    
    public void ChangeToTitleBGM()
    {
        ChangeBGM(titleBGM);
    }
}
```

**사용법**:
```csharp
// MainMenuManager 또는 버튼에서 호출
BGMManager.Instance.ChangeToGameplayBGM();
```

### 방법 3: Resources를 이용한 동적 로드

```csharp
public void ChangeBGMFromResources(string clipName)
{
    AudioClip newClip = Resources.Load<AudioClip>($"BGM/{clipName}");
    if (newClip != null && bgmAudioSource != null)
    {
        bgmAudioSource.clip = newClip;
        bgmAudioSource.Play();
    }
}
```

## Unity 에디터에서 설정하는 방법

### 1단계: AudioSource 찾기
1. Unity 에디터에서 `Spot_Title` 프리팹 열기
2. Hierarchy에서 "Neosign Dudungtak!"을 재생하는 AudioSource 찾기
   - 보통 씬 레벨이나 별도 GameObject에 존재
   - 또는 `GameObject.Find("BGM")` 등으로 찾을 수 있음

### 2단계: 스크립트 수정 및 연결
1. `MainMenuManager.cs`에 `ChangeBGM()` 메서드 추가
2. `MainMenuManager` 컴포넌트 선택
3. Inspector에서:
   - `Bgm Audio Source`: AudioSource 연결
   - `New BGM Clip`: "Dudungtak! Neo Street-2.mp3" 드래그 앤 드롭

### 3단계: 버튼 On Click 설정
1. `play_btn` 선택
2. Button 컴포넌트의 `On Click ()` 섹션
3. `+` 버튼 클릭하여 새 이벤트 추가
4. `MainMenuManager` 오브젝트 드래그
5. 드롭다운에서 `MainMenuManager > ChangeBGM()` 선택

## 권장 구현 순서

1. **씬에서 AudioSource 찾기**
   - Main 씬을 열고 BGM을 재생하는 AudioSource GameObject 확인
   - 이름이나 태그 확인

2. **MainMenuManager 수정**
   - `ChangeBGM()` 메서드 추가
   - AudioSource와 새 클립 필드 추가

3. **Unity 에디터에서 연결**
   - MainMenuManager에 AudioSource 참조 연결
   - 새 BGM 클립 연결

4. **버튼 이벤트 설정**
   - play_btn의 On Click에 ChangeBGM() 추가
   - 실행 순서 확인 (BGM 변경 후 이동 또는 동시 실행)

## 추가 고려사항

- **재생 중 전환**: 현재 BGM을 멈추고 새 BGM 재생
- **페이드 인/아웃**: 부드러운 전환을 원하면 코루틴으로 페이드 처리
- **Loop 설정**: 새 BGM도 Loop 설정 확인
