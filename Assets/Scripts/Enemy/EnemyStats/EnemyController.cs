using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public EnemyStatsSO enemyData;      // Im Inspector: Slime SO, Goblin SO, etc.
    public EnemyInstance runtimeData;   // Live Daten

    void Start()
    {
        runtimeData = new EnemyInstance { baseData = enemyData };
        runtimeData.Initialize();
    }
}