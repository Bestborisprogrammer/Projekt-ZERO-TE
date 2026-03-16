[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public int modifier;
    public int turnsRemaining;

    public StatModifier(StatType type, int mod, int duration)
    {
        statType = type;
        modifier = mod;
        turnsRemaining = duration;
    }
}