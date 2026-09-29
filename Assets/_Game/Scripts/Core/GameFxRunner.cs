using System.Collections.Generic;
using UnityEngine;

namespace CasualGame.Core
{
    /// <summary>Scene object that parents pooled effects and hands them back to GameFx when they finish.</summary>
    internal sealed class GameFxRunner : MonoBehaviour
    {
        private readonly List<(FxEffect fx, float until)> live = new();

        public static GameFxRunner Create() => new GameObject("GameFx").AddComponent<GameFxRunner>();

        public void Track(FxEffect fx, float seconds) => live.Add((fx, Time.time + seconds));

        /// <summary>Ends every running effect now (screen change).</summary>
        public void StopAll()
        {
            foreach (var (fx, _) in live) GameFx.Return(fx);
            live.Clear();
        }

        private void Update()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                if (Time.time < live[i].until) continue;
                GameFx.Return(live[i].fx);
                live.RemoveAt(i);
            }
        }
    }
}
