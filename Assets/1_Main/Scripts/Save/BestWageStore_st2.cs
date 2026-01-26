using UnityEngine;

public static class BestWageStore_st2
{
    private static string Key(string stageId) => $"best_wage_{stageId}";
    private static string TimeKey(string stageId) => $"best_wage_time_{stageId}";

    /// <summary>
    /// 최고기록 갱신 시도. currentWage가 더 크면 저장하고 true 반환.
    /// </summary>
    public static bool TryUpdateBest(string stageId, int currentWage)
    {
        int best = GetBest(stageId);
        if (currentWage <= best) return false;

        PlayerPrefs.SetInt(Key(stageId), currentWage);
        PlayerPrefs.SetString(TimeKey(stageId), System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        PlayerPrefs.Save();
        return true;
    }

    public static int GetBest(string stageId) => PlayerPrefs.GetInt(Key(stageId), 0);
    public static string GetBestTime(string stageId) => PlayerPrefs.GetString(TimeKey(stageId), "");

    public static void Clear(string stageId)
    {
        PlayerPrefs.DeleteKey(Key(stageId));
        PlayerPrefs.DeleteKey(TimeKey(stageId));
        PlayerPrefs.Save();
    }
}
