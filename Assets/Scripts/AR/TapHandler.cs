using System;
using UnityEngine;

namespace GuardianAR
{
    /// <summary>
    /// AR 오브젝트 터치/클릭 감지
    /// </summary>
    public class TapHandler : MonoBehaviour
    {
        public event Action OnTapped;

        void OnMouseDown() => OnTapped?.Invoke();
    }
}
