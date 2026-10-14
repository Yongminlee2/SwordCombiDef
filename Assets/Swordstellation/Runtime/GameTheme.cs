using UnityEngine;

namespace Swordstellation.Runtime
{
    public static class GameTheme
    {
        // Night / Nebula / Starlight / Muted / EmberGold (numbers only) /
        // ConstellationCyan (actionable now) / WarningCoral (enemy pressure).
        public static readonly Color Background = FromHex(0x0E1430);
        public static readonly Color BackgroundLight = FromHex(0x1A2440);
        public static readonly Color Panel = FromHex(0x2A2358);
        public static readonly Color PanelLight = FromHex(0x2E3D64);
        public static readonly Color Gold = FromHex(0xFFC24A);
        public static readonly Color Cyan = FromHex(0x6FE3F0);
        public static readonly Color Mint = FromHex(0x72D6A0);
        public static readonly Color Coral = FromHex(0xFF6B6B);
        public static readonly Color Violet = FromHex(0x9B7ADB);
        public static readonly Color Text = FromHex(0xF3F1FF);
        public static readonly Color MutedText = FromHex(0xB9C2DE);
        public static readonly Color Slot = FromHex(0x162039);
        public static readonly Color SlotSelected = FromHex(0x4C5E91);

        public static Color FromHex(int rgb, float alpha = 1f)
        {
            float red = ((rgb >> 16) & 0xFF) / 255f;
            float green = ((rgb >> 8) & 0xFF) / 255f;
            float blue = (rgb & 0xFF) / 255f;
            return new Color(red, green, blue, alpha);
        }
    }
}

