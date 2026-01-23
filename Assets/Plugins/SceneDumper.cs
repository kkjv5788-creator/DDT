#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

public class ProjectDumper : MonoBehaviour
{
    [MenuItem("Tools/Dump Scene and Active Scripts Only")]
    static void ExportAllInfo()
    {
        StringBuilder sb = new StringBuilder();
        
        // ---------------------------------------------------------
        // 1. 씬 정보 추출
        // ---------------------------------------------------------
        sb.AppendLine($"[PROJECT DUMP START]");
        sb.AppendLine($"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        sb.AppendLine($"Date: {System.DateTime.Now}");
        sb.AppendLine("========================================");
        sb.AppendLine("              SCENE HIERARCHY");
        sb.AppendLine("========================================\n");

        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        
        // 스크립트 경로를 중복 없이 저장할 HashSet
        HashSet<string> activeScriptPaths = new HashSet<string>();

        foreach (GameObject obj in rootObjects)
        {
            DumpGameObject(obj, sb, "", activeScriptPaths);
        }

        // ---------------------------------------------------------
        // 2. 현재 씬에서 사용 중인 스크립트 코드만 추출
        // ---------------------------------------------------------
        DumpActiveScripts(sb, activeScriptPaths);

        // ---------------------------------------------------------
        // 3. 파일 저장 및 실행
        // ---------------------------------------------------------
        string filename = "ProjectFullDump_ActiveOnly.txt";
        string path = Path.Combine(Application.dataPath, "..", filename);
        
        File.WriteAllText(path, sb.ToString());
        Debug.Log($"Dump saved to: {path}");
        Application.OpenURL(path);
    }

    // 재귀적으로 하이어라키를 탐색하며 정보 기록 + 사용된 스크립트 수집
    static void DumpGameObject(GameObject obj, StringBuilder sb, string indent, HashSet<string> scriptPaths)
    {
        bool isActive = obj.activeInHierarchy;
        string status = isActive ? "[O]" : "[X]";
        sb.AppendLine($"{indent}{status} {obj.name} (Tag: {obj.tag}, Layer: {LayerMask.LayerToName(obj.layer)})");

        // 컴포넌트 정보 기록 및 스크립트 경로 수집
        Component[] components = obj.GetComponents<Component>();
        foreach (Component c in components)
        {
            if (c == null) continue; // Missing Script 방지

            string compName = c.GetType().Name;
            
            // MonoBehaviour인 경우 실제 스크립트 파일 경로를 찾음
            if (c is MonoBehaviour mb)
            {
                MonoScript script = MonoScript.FromMonoBehaviour(mb);
                if (script != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(script);
                    // "Assets/"로 시작하고, 내부 엔진 코드나 플러그인이 아닌 경우만 수집
                    if (IsValidScriptPath(assetPath))
                    {
                        scriptPaths.Add(assetPath);
                    }
                }
            }

            // (선택) 하이어라키 뷰에 컴포넌트 목록도 간단히 표시하려면 아래 주석 해제
            // sb.AppendLine($"{indent}    - (Component) {compName}");
        }

        // 자식 오브젝트 탐색
        foreach (Transform child in obj.transform)
        {
            DumpGameObject(child.gameObject, sb, indent + "    ", scriptPaths);
        }
    }

    // 수집된 경로의 스크립트 내용 덤프
    static void DumpActiveScripts(StringBuilder sb, HashSet<string> paths)
    {
        sb.AppendLine("\n========================================");
        sb.AppendLine("         USED SCRIPTS SOURCE CODE");
        sb.AppendLine("   (Only scripts attached in this Scene)");
        sb.AppendLine("========================================\n");

        // 경로 알파벳순 정렬
        var sortedPaths = paths.OrderBy(p => p).ToList();

        if (sortedPaths.Count == 0)
        {
            sb.AppendLine("No custom scripts found in this scene.");
            return;
        }

        foreach (string relativePath in sortedPaths)
        {
            string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);

            if (File.Exists(fullPath))
            {
                sb.AppendLine($"--- FILENAME: {Path.GetFileName(relativePath)} ({relativePath}) ---");
                sb.AppendLine(File.ReadAllText(fullPath));
                sb.AppendLine("\n--------------------------------------------------\n");
            }
        }
    }

    // 덤프에서 제외할 경로 필터링
    static bool IsValidScriptPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        if (!path.StartsWith("Assets/")) return false; // 패키지나 내부 코드는 제외
        if (!path.EndsWith(".cs")) return false;

        // 제외하고 싶은 폴더 키워드
        if (path.Contains("/Plugins/")) return false;
        if (path.Contains("/Editor/")) return false;
        if (path.Contains("/TextMesh Pro/")) return false;
        if (path.Contains("/Oculus/")) return false; // 오큘러스 SDK 코드 제외 (너무 많음)

        return true;
    }
}
#endif