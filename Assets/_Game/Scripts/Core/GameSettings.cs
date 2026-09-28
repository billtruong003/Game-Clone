using System;

namespace CasualGame.Core
{
    public static class GameSettings
    {
        public static event Action Changed;

        public static bool Sound
        {
            get => SaveStore.GetBool("set.sound", true);
            set { SaveStore.SetBool("set.sound", value); Changed?.Invoke(); }
        }

        public static bool Music
        {
            get => SaveStore.GetBool("set.music", true);
            set { SaveStore.SetBool("set.music", value); Changed?.Invoke(); }
        }

        public static bool Vibration
        {
            get => SaveStore.GetBool("set.vibration", true);
            set { SaveStore.SetBool("set.vibration", value); Changed?.Invoke(); }
        }
    }
}
