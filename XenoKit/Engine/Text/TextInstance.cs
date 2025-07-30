using Microsoft.Xna.Framework;

namespace XenoKit.Engine.Text
{
    public readonly struct TextInstance
    {
        public readonly string Text;
        public readonly Color Color;
        public readonly Vector2 ScreenPosition;
        public readonly bool UseBackground;

        public TextInstance(string text, Color color, Vector2 screenPosition, bool useBackground)
        {
            Text = text;
            Color = color;
            ScreenPosition = screenPosition;
            UseBackground = useBackground;
        }

    }
}
