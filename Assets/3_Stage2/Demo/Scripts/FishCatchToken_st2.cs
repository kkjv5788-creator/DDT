using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishCatchToken_st2 : MonoBehaviour
{
    [Header("판정 데이터")]
    public double popTime;
    public bool isResolved = false;
    public bool isCaught = false;
    public OVRInput.Controller? assignedHand = null;

    [Header("이동 데이터")]
    private Vector3 startPos;
    private Vector3 endPos;
    private Transform parentMold;

    // ✅ 추가: 소유 몰드 추적
    public MoldController_st2 ownerMold;

    // 초기화
    public void Initialize(double popDspTime, Transform mold, MoldController_st2 owner)
    {
        popTime = popDspTime;
        isResolved = false;
        isCaught = false;
        assignedHand = null;
        parentMold = mold;
        ownerMold = owner; // ✅ 소유 몰드 저장

        // 시작/끝 위치 설정
        startPos = mold.position + Vector3.up * 1f;
        endPos = mold.position + Vector3.up * 0.07f;
        transform.position = startPos;

        // 플레이어 방향 회전
        Transform playerCam = Camera.main.transform;
        Vector3 toPlayer = (playerCam.position - transform.position).normalized;
        transform.right = toPlayer;
    }

    // fish 움직임 업데이트
    public void UpdateMovement()
    {
        if (isResolved) return;

        float elapsed = (float)(AudioSettings.dspTime - popTime);

        if (elapsed < 0.8f)
        {
            // 하강 (0~0.8초)
            float t = elapsed / 0.8f;
            transform.position = Vector3.Lerp(startPos, endPos, t);
        }
        else if (elapsed < 1.0f)
        {
            // 대기 (0.8~1.0초)
            transform.position = endPos;
        }
    }
}