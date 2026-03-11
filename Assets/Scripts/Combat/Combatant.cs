// Einheitliche Klasse damit Turn Order für beide funktioniert
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

    // Referenzen für spätere Updates
    private CharacterInstance characterRef;
    private EnemyInstance enemyRef;

    // Party Member Konstruktor
    public Combatant(CharacterInstance c)
    {
        characterRef = c;
        IsEnemy = false;
        Refresh();
        XPReward = 0;
    }

    // Enemy Konstruktor
    public Combatant(EnemyInstance e)
    {
        enemyRef = e;
        IsEnemy = true;
        Refresh();
        XPReward = e.XPReward;
    }

    public void TakeDamage(int damage)
    {
        if (IsEnemy) enemyRef.TakeDamage(damage);
        else characterRef.TakeDamage(damage);
        Refresh();
    }

    // Stats aktuell halten
    void Refresh()
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