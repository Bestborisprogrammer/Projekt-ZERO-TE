public class Combatant
{
    public string Name { get; private set; }
    public int Speed { get; private set; }
    public int Attack { get; private set; }
    public int Defense { get; private set; }
    public int MaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public int XPReward { get; private set; }
    public bool IsEnemy { get; private set; }
    public bool IsAlive => CurrentHP > 0;

    private CharacterInstance characterRef;
    private EnemyInstance enemyRef;

    public Combatant(CharacterInstance c)
    {
        characterRef = c;
        IsEnemy = false;
        XPReward = 0;
        Refresh();
    }

    public Combatant(EnemyInstance e)
    {
        enemyRef = e;
        IsEnemy = true;
        XPReward = e.XPReward;
        Refresh();
    }

    public void TakeDamage(int damage)
    {
        if (IsEnemy) enemyRef.TakeDamage(damage);
        else characterRef.TakeDamage(damage);
        Refresh();
    }

    public void Refresh()
    {
        if (IsEnemy)
        {
            Name = enemyRef.Name;
            Speed = enemyRef.Speed;
            Attack = enemyRef.Attack;
            Defense = enemyRef.Defense;
            MaxHP = enemyRef.MaxHP;
            CurrentHP = enemyRef.currentHP;
        }
        else
        {
            Name = characterRef.Name;
            Speed = characterRef.Speed;
            Attack = characterRef.Attack;
            Defense = characterRef.Defense;
            MaxHP = characterRef.MaxHP;
            CurrentHP = characterRef.currentHP;
        }
    }
}