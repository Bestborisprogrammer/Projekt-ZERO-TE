using System.Collections.Generic;

[System.Serializable]
public class ActiveStatusEffect
{
    public StatusEffectType type;
    public int turnsRemaining;
    public float dotPercent;

    public ActiveStatusEffect(StatusEffectType type, int duration, float dotPercent = 0f)
    {
        this.type = type;
        this.turnsRemaining = duration;
        this.dotPercent = dotPercent;
    }
}