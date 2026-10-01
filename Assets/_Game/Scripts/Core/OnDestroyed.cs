using System;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>Runs an action when its GameObject goes away, however that happens (closed, replaced, scene change).</summary>
    public sealed class OnDestroyed : MonoBehaviour
    {
        public Action Action;

        private void OnDestroy() => Action?.Invoke();
    }
}
