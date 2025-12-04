using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    public GameObject[] monsterPrefabs;
    public Transform player;
    public float spawnDistance = 12f;
    public float minSpawnTime = 3f;
    public float maxSpawnTime = 7f;
    public float minY = -4f;
    public float maxY = -2f;

    private float timer;

    void Start() => ResetTimer();

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnMonster();
            ResetTimer();
        }
    }

    void ResetTimer() => timer = Random.Range(minSpawnTime, maxSpawnTime);

    void SpawnMonster()
    {
        if (monsterPrefabs.Length == 0) return;

        int idx = Random.Range(0, monsterPrefabs.Length);
        GameObject prefab = monsterPrefabs[idx];

        float y = Random.Range(minY, maxY);
        Vector3 spawnPos = new Vector3(player.position.x + spawnDistance, y, 0);

        GameObject monster = Instantiate(prefab, spawnPos, Quaternion.identity);

        MonsterSequence ms = monster.GetComponent<MonsterSequence>();
        if (ms != null)
        {
            // movement normal tetap sama
            ms.moveSpeed = Random.Range(0.8f, 1.2f);

            // -------------------------------------------
            // PATCH: 10% chance menjadi irregular monster
            // -------------------------------------------
            if (Random.value < 0.10f)
            {
                ms.isIrregular = true;      // Flag irregular
                ms.moveSpeed *= 1.4f;       // Sedikit lebih cepat
                ms.irregularAmplitude = 0.3f;  // Zigzag kecil
                ms.irregularFrequency = 3f;    // Kecepatan zigzag
            }

            // tetap aktifkan sequence seperti biasa
            ms.ActivateSequence();
        }
    }
}
