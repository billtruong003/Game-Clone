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
