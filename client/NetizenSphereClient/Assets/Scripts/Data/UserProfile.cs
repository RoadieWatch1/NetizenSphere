using System;

namespace NetizenSphere.Data
{
    [Serializable]
    public class UserProfile
    {
        public string UserId;
        public string DisplayName;
        public string AvatarPrimaryColor;   // hex, e.g. "#00FFFF"
        public string AvatarAccentColor;    // hex, e.g. "#FFFFFF"
        public string AvatarPreset;         // null until avatar presets are added
        public DateTime LastLoginAt;
    }
}
