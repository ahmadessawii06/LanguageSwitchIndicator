namespace LanguageSwitchIndicator.Models;

public sealed record AnimationPreset(string Name, AnimationStyle Style, double DisplayMs, double FadeInMs, double FadeOutMs)
{
    public static IReadOnlyList<AnimationPreset> All { get; } = new[]
    {
        new AnimationPreset("شكل ماك", AnimationStyle.FadeScale, 1000, 180, 250),
        new AnimationPreset("سريع جدًا", AnimationStyle.Fade, 350, 50, 80),
        new AnimationPreset("طبيعي", AnimationStyle.Fade, 700, 120, 200),
        new AnimationPreset("بطيء", AnimationStyle.FadeScale, 1600, 350, 500)
    };

    public bool Matches(SettingsModel settings) =>
        settings.AnimationStyle == Style && settings.PopupDurationMs == DisplayMs &&
        settings.FadeInDurationMs == FadeInMs && settings.FadeOutDurationMs == FadeOutMs;

    public void Apply(SettingsModel settings)
    {
        settings.AnimationStyle = Style;
        settings.PopupDurationMs = DisplayMs;
        settings.FadeInDurationMs = FadeInMs;
        settings.FadeOutDurationMs = FadeOutMs;
    }
}
