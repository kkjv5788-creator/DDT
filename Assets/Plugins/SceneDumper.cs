#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Reflection;

public class SceneDumper : MonoBehaviour
{
    [MenuItem("Tools/Export Scene Info to Text")]
    static void ExportSceneInfo()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Scene Dump: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        sb.AppendLine($"Date: {System.DateTime.Now}");
        sb.AppendLine("========================================\n");

        // 씬의 루트 오브젝트들 가져오기
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        foreach (GameObject obj in rootObjects)
        {
            DumpGameObject(obj, sb, "");
        }

        // 파일로 저장
        string path = Path.Combine(Application.dataPath, "../SceneDump.txt");
        File.WriteAllText(path, sb.ToString());

        // 완료 알림 및 파일 열기
        EditorUtility.DisplayDialog("완료", $"프로젝트 폴더(Assets 상위)에 'SceneDump.txt'가 생성되었습니다.\n\n경로: {path}", "확인");
        System.Diagnostics.Process.Start(path);
    }

    static void DumpGameObject(GameObject obj, StringBuilder sb, string indent)
    {
        // 1. 오브젝트 기본 정보
        sb.AppendLine($"{indent}[O] {obj.name} (Active: {obj.activeSelf}, Tag: {obj.tag}, Layer: {LayerMask.LayerToName(obj.layer)})");
        
        // 2. 트랜스폼 정보 (위치/회전/크기)
        sb.AppendLine($"{indent}    Position: {obj.transform.position}");
        sb.AppendLine($"{indent}    Rotation: {obj.transform.eulerAngles}");
        sb.AppendLine($"{indent}    Scale:    {obj.transform.localScale}");

        // 3. 컴포넌트 및 인스펙터 값 정보
        Component[] components = obj.GetComponents<Component>();
        foreach (Component c in components)
        {
            if (c == null) continue; // Missing Script 방지
            if (c is Transform) continue; // 트랜스폼은 위에서 적었으니 패스

            string compName = c.GetType().Name;
            sb.AppendLine($"{indent}    - (Component) {compName}");

            // Reflection을 사용하여 Public 변수 값 긁어오기
            FieldInfo[] fields = c.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            if (fields.Length > 0)
            {
                foreach (FieldInfo field in fields)
                {
                    object value = field.GetValue(c);
                    sb.AppendLine($"{indent}        > {field.Name}: {value}");
                }
            }
        }

        sb.AppendLine(); // 공백

        // 4. 자식 오브젝트 재귀 호출
        foreach (Transform child in obj.transform)
        {
            DumpGameObject(child.gameObject, sb, indent + "    "); // 들여쓰기 추가
        }
    }
}
#endif