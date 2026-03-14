[System.Serializable]
public class ActiveStatusEffect
{
    public StatusEffectType type;
    public int turnsRemaining;
    public float dotPercent;
    public float defenseReduction; // for Dark
    public int speedReduction;     // for Wet

    public ActiveStatusEffect(StatusEffectType type, int duration, float dotPercent = 0f, float defenseReduction = 0f, int speedReduction = 0)
    {
        this.type = type;
        this.turnsRemaining = duration;
        this.dotPercent = dotPercent;
        this.defenseReduction = defenseReduction;
        this.speedReduction = speedReduction;
    }
}