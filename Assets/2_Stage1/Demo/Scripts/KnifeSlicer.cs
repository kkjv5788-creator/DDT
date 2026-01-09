using UnityEngine;
using Project.Core;
using Project.Gameplay.Kimbap;

namespace Project.Gameplay.Knife
{
    [RequireComponent(typeof(Collider))]
    public class KnifeSlicer : MonoBehaviour
    {
        [Header("Refs")]
        public KnifeVelocityEstimator velocityEstimator;

        [Header("Fail spam control")]
        public float failCooldownOverride = -1f; // if <0 uses FeedbackSetSO default via FeedbackRouter

        // Attempt state
        bool attemptActive;
        bool canSlice = true;
        float lastSliceTime = -999f;
        float contactStartTime;
        float lastFailTime = -999f;

        KimbapSliceTarget currentTarget;
        Collider currentTrigger;

        void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
            if (!velocityEstimator) velocityEstimator = GetComponentInChildren<KnifeVelocityEstimator>();
        }

        void Update()
        {
            // auto unlock via cooldown (Exit 없어도 가능)
            if (!canSlice)
            {
                if (Time.time - lastSliceTime >= KnifeSlicerRuntime.SliceCooldownSeconds)
                    canSlice = true;
            }

            // If we are inside trigger, we can evaluate in Update too (safer than relying on Stay)
            if (attemptActive && currentTarget && currentTrigger)
            {
                TryEvaluate(currentTrigger);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other) return;

            var target = other.GetComponentInParent<KimbapSliceTarget>();
            if (!target) return;

            attemptActive = true;
            currentTarget = target;
            currentTrigger = other;
            contactStartTime = Time.time;

            GameEvents.RaiseSliceAttemptStarted(target);

            // Attempt starts; do not reset canSlice here (keeps cooldown rule consistent)
            TryEvaluate(other);
        }

        void OnTriggerExit(Collider other)
        {
            if (other != currentTrigger) return;

            attemptActive = false;
            currentTarget = null;
            currentTrigger = null;

            // Option A: Exit unlock (allowed)
            canSlice = true;
        }

        void TryEvaluate(Collider trigger)
        {
            if (!currentTarget) { Fail(trigger, SliceFailReason.NoTarget); return; }

            if (!KnifeSlicerRuntime.IsJudging) { Fail(trigger, SliceFailReason.NotJudging); return; }

            if (!canSlice) return;

            float speed = velocityEstimator ? velocityEstimator.CurrentSpeed : 0f;
            if (speed < KnifeSlicerRuntime.MinKnifeSpeed)
            {
                Fail(trigger, SliceFailReason.SpeedTooLow);
                return;
            }

            if (KnifeSlicerRuntime.UseContactTime)
            {
                int ms = Mathf.RoundToInt((Time.time - contactStartTime) * 1000f);
                if (ms < KnifeSlicerRuntime.MinContactMs)
                {
                    Fail(trigger, SliceFailReason.ContactTooShort);
                    return;
                }
            }

            // SUCCESS (Attempt 동안 첫 1회만)
            canSlice = false;
            lastSliceTime = Time.time;

            // Hit position/normal
            Vector3 hitPos = trigger.ClosestPoint(transform.position);
            Vector3 hitNormal = (transform.position - hitPos).normalized;

            // Execute slice only on success
            currentTarget.Controller.TrySliceRightThin(hitPos, hitNormal);

            GameEvents.RaiseSliceSuccess(hitPos, hitNormal, speed);
        }

        void Fail(Collider trigger, SliceFailReason reason)
        {
            float failCooldown = (failCooldownOverride > 0f) ? failCooldownOverride : 0.2f;
            if (Time.time - lastFailTime < failCooldown) return;
            lastFailTime = Time.time;

            Vector3 pos = trigger ? trigger.ClosestPoint(transform.position) : transform.position;
            GameEvents.RaiseSliceFail(pos, reason);
        }
    }
}
