using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CompleteVRAutoSetup : MonoBehaviour
{
    [Header("완전한 VR 자동 설정 (최종 수정됨)")]
    [Space(10)]
    [SerializeField] private bool _executeCompleteSetup;
    
    void Start() { } // 자동 실행 방지

    [ContextMenu("완전한 VR 설정 실행")]
    public void ExecuteCompleteSetup()
    {
        Debug.Log("=== 🚀 VR 자동 설정을 시작합니다 ===");
        
        FixControllerPositions();
        FixControllerDataSources();
        FixControllerVisuals();
        FixRayInteractors();
        AddMissingRayComponents();
        FixCanvasSettings();
        FixUIInteraction();
        FixEventSystem();
        
        Debug.Log("=== ✨ 모든 설정이 완료되었습니다! Play를 눌러 확인하세요. ===");
    }

    // [1] 컨트롤러 위치 수정
    public void FixControllerPositions()
    {
        GameObject cameraRig = FindCameraRig();
        if (cameraRig == null) return;

        Transform leftAnchor = FindDeepChild(cameraRig.transform, "LeftControllerAnchor");
        Transform rightAnchor = FindDeepChild(cameraRig.transform, "RightControllerAnchor");
        
        if (leftAnchor == null) leftAnchor = FindDeepChild(cameraRig.transform, "LeftHandAnchor");
        if (rightAnchor == null) rightAnchor = FindDeepChild(cameraRig.transform, "RightHandAnchor");

        MoveControllerVisual("OVRLeftControllerVisual", leftAnchor);
        MoveControllerVisual("OVRRightControllerVisual", rightAnchor);
    }

    private void MoveControllerVisual(string visualName, Transform targetParent)
    {
        GameObject visual = GameObject.Find(visualName);
        if (visual != null && targetParent != null)
        {
            #if UNITY_EDITOR
            if (PrefabUtility.IsPartOfPrefabInstance(visual))
                PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            #endif
            
            visual.transform.SetParent(targetParent);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            Debug.Log($"✅ 위치 이동 완료: {visualName}");
        }
    }

    // [2] 데이터 소스 수정
    public void FixControllerDataSources()
    {
        var dataSources = FindObjectsOfType<MonoBehaviour>().Where(mb => mb.GetType().Name == "FromOVRControllerDataSource");
        foreach (var component in dataSources) SetFieldValue(component, "_updateMode", 2);
    }

    // [3] Visual 항상 보이게 수정
    public void FixControllerVisuals()
    {
        var controllerHelpers = FindObjectsOfType<MonoBehaviour>().Where(mb => mb.GetType().Name == "OVRControllerHelper");
        foreach (var component in controllerHelpers) SetFieldValue(component, "m_showState", 0);
    }

    // [4] 레이저(Ray) 설정 수정
    public void FixRayInteractors()
    {
        var rayInteractors = FindObjectsOfType<MonoBehaviour>().Where(mb => mb.GetType().Name.Contains("RayInteractor"));
        foreach (var ray in rayInteractors)
        {
            SetFieldValue(ray, "_maxRayLength", 20f); // 길이 늘림
            SetFieldValue(ray, "maxRayLength", 20f);
            SetFieldValue(ray, "_enableInteractionWithUIGameObjects", true);
        }
        
        var rayVisuals = FindObjectsOfType<MonoBehaviour>().Where(mb => mb.GetType().Name.Contains("ControllerRayVisual"));
        foreach (var visual in rayVisuals)
        {
            SetFieldValue(visual, "_maxRayVisualLength", 20f);
            SetFieldValue(visual, "maxRayVisualLength", 20f);
            SetFieldValue(visual, "_hideWhenNoInteractable", false); // 항상 보이게
            SetFieldValue(visual, "hideWhenNoInteractable", false);
        }
    }

    // [5] 레이저 선 그리기 (오류 수정된 버전)
    public void AddMissingRayComponents()
    {
        var controllerRays = FindObjectsOfType<Transform>().Where(t => t.name.Contains("ControllerRay")).ToArray();
        
        foreach (var controllerRay in controllerRays)
        {
            LineRenderer line = controllerRay.GetComponent<LineRenderer>();
            if (line == null)
            {
                line = controllerRay.gameObject.AddComponent<LineRenderer>();
                
                // 재질 설정 (없으면 기본값)
                Material defaultMat = new Material(Shader.Find("Sprites/Default"));
                line.material = defaultMat;
                
                // [중요] 여기가 수정되었습니다 (.color 삭제 -> startColor/endColor 사용)
                line.startColor = Color.cyan;
                line.endColor = new Color(0, 1, 1, 0); // 끝은 투명하게
                
                line.startWidth = 0.005f;
                line.endWidth = 0.001f;
                line.positionCount = 2;
                line.useWorldSpace = true;
                
                Debug.Log($"✅ 레이저 시각화 추가: {controllerRay.name}");
            }
        }
    }

    // [6] 캔버스 설정 (Type Null 오류 수정됨)
    public void FixCanvasSettings()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                // 이벤트 카메라 연결
                Camera centerEye = FindCenterEyeCamera();
                if (centerEye != null) canvas.worldCamera = centerEye;
                
                // GraphicRaycaster
                if (canvas.GetComponent<GraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                
                // [중요] 안전하게 타입 찾기
                System.Type pointableType = FindTypeInAssemblies("PointableCanvasModule");
                if (pointableType != null)
                {
                    if (canvas.GetComponent(pointableType) == null)
                    {
                        canvas.gameObject.AddComponent(pointableType);
                        Debug.Log($"✅ PointableCanvasModule 추가됨: {canvas.name}");
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ PointableCanvasModule 스크립트를 찾을 수 없습니다. Meta SDK가 설치되어 있나요?");
                }
            }
        }
    }

    // [7] UI 상호작용
    public void FixUIInteraction()
    {
        Button[] buttons = FindObjectsOfType<Button>();
        System.Type feedbackType = FindTypeInAssemblies("ButtonFeedback");

        foreach (Button button in buttons)
        {
            if (feedbackType != null && button.GetComponent(feedbackType) != null)
            {
                if (button.GetComponent<AudioSource>() == null)
                {
                    var source = button.gameObject.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                }
            }
        }
    }

    // [8] 이벤트 시스템 정리
    public void FixEventSystem()
    {
        var eventSystems = FindObjectsOfType<EventSystem>();
        if (eventSystems.Length > 1)
        {
            for (int i = 1; i < eventSystems.Length; i++)
                DestroyImmediate(eventSystems[i].gameObject);
        }
        
        EventSystem es = FindObjectOfType<EventSystem>();
        if (es != null)
        {
             // OVRInputModule 확인 (PointableCanvasInputModule이 없을 경우 대비)
             // 여기서 굳이 강제로 넣지 않고 놔둡니다. (Interaction SDK가 알아서 처리)
        }
    }

    // === 유틸리티 함수 ===

    // 안전하게 타입 찾기 (네임스페이스 무시)
    private System.Type FindTypeInAssemblies(string typeName)
    {
        var type = System.Type.GetType(typeName);
        if (type != null) return type;

        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
            if (type != null) return type;
        }
        return null;
    }

    private void SetFieldValue(object obj, string fieldName, object value)
    {
        if (obj == null) return;
        var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field?.SetValue(obj, value);
    }

    private GameObject FindCameraRig()
    {
        var found = GameObject.Find("[BuildingBlock] Camera Rig");
        if (found == null) found = GameObject.Find("OVRCameraRig");
        return found;
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    private Camera FindCenterEyeCamera()
    {
        var camObj = GameObject.Find("CenterEyeAnchor");
        if (camObj != null) return camObj.GetComponent<Camera>();
        return Camera.main;
    }
}