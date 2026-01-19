#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;

public class ProjectDumper : MonoBehaviour
{
    [MenuItem("Tools/Dump Scene and Scripts")]
    static void ExportAllInfo()
    {
        StringBuilder sb = new StringBuilder();
        
        // ---------------------------------------------------------
        // 1. 씬 정보 추출 (기존 기능)
        // ---------------------------------------------------------
        sb.AppendLine($"[PROJECT DUMP START]");
        sb.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        sb.AppendLine($"Date: {System.DateTime.Now}");
        sb.AppendLine("========================================");
        sb.AppendLine("              SCENE HIERARCHY");
        sb.AppendLine("========================================\n");

        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            DumpGameObject(obj, sb, "");
        }

        // ---------------------------------------------------------
        // 2. 스크립트 코드 추출 (추가된 기능)
        // ---------------------------------------------------------
        DumpScripts(sb);

        // ---------------------------------------------------------
        // 3. 파일 저장 및 실행
        // ---------------------------------------------------------
        string path = Path.Combine(Application.dataPath, "../ProjectFullDump.txt");
        File.WriteAllText(path, sb.ToString());

        EditorUtility.DisplayDialog("완료", $"프로젝트 폴더(Assets 상위)에 'ProjectFullDump.txt'가 생성되었습니다.\n\n경로: {path}", "확인");
        System.Diagnostics.Process.Start(path);
    }

    // --- [기존] 씬 구조 및 값 추출 함수 ---
    static void DumpGameObject(GameObject obj, StringBuilder sb, string indent)
    {
        sb.AppendLine($"{indent}[O] {obj.name} (Active: {obj.activeSelf}, Tag: {obj.tag}, Layer: {LayerMask.LayerToName(obj.layer)})");
        sb.AppendLine($"{indent}    Pos: {obj.transform.position} | Rot: {obj.transform.eulerAngles} | Scale: {obj.transform.localScale}");

        Component[] components = obj.GetComponents<Component>();
        foreach (Component c in components)
        {
            if (c == null) continue;
            if (c is Transform) continue;

            string compName = c.GetType().Name;
            sb.AppendLine($"{indent}    - (Component) {compName}");

            // Public 변수 값 긁어오기
            FieldInfo[] fields = c.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (fields.Length > 0)
            {
                foreach (FieldInfo field in fields)
                {
                    try {
                        object value = field.GetValue(c);
                        sb.AppendLine($"{indent}        > {field.Name}: {value}");
                    } catch { }
                }
            }
        }
        sb.AppendLine();

        foreach (Transform child in obj.transform)
        {
            DumpGameObject(child.gameObject, sb, indent + "    ");
        }
    }

    // --- [신규] 스크립트 코드 수집 함수 ---
    static void DumpScripts(StringBuilder sb)
    {
        sb.AppendLine("\n========================================");
        sb.AppendLine("              SCRIPTS SOURCE CODE");
        sb.AppendLine("========================================\n");

        // Assets 폴더 내의 모든 .cs 파일 검색
        string[] allScriptPaths = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

        foreach (string filePath in allScriptPaths)
        {
            // 경로 통일 (Windows 역슬래시 문제 방지)
            string relativePath = "Assets" + filePath.Substring(Application.dataPath.Length).Replace("\\", "/");

            // [필터링] 제외하고 싶은 폴더 키워드 설정
            // 외부 에셋이나 엔진 관련 코드는 제외하고 내가 짠 코드만 보기 위함
            if (relativePath.Contains("/Plugins/") || 
                relativePath.Contains("/Editor/") || 
                relativePath.Contains("/TextMesh Pro/") ||
                relativePath.Contains("/Lib/") ||
                relativePath.Contains("/Standard Assets/"))
            {
                continue;
            }

            sb.AppendLine($"--- FILENAME: {Path.GetFileName(relativePath)} ({relativePath}) ---");
            sb.AppendLine(File.ReadAllText(filePath));
            sb.AppendLine("\n--------------------------------------------------\n");
        }
    }
}
#endif