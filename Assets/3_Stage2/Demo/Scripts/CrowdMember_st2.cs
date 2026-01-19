using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class CrowdMember_st2 : MonoBehaviour
{
    [Serializable]
    public class CrowdMember
    {
        public GameObject gameObject;
        public Renderer cachedRenderer;
        public Animator cachedAnimator;
    }
}
