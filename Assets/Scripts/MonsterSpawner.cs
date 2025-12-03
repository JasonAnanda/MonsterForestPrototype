using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    public GameObject[] monsterPrefabs;
    public Transform player;
    public float spawnDistance = 12f;
    public float minSpawnTime = 6f;
    public float maxSpawnTime = 8f;
    public float minY = -4f;
    public float maxY = -2f;

    [Header("Auto assign")]
    public string deathLineName = "DeathLineInstance"; // name of the instance in scene

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

        // cari deathLine instance di scene
        GameObject deathObj = GameObject.Find(deathLineName);
        Transform deathT = (deathObj != null) ? deathObj.transform : null;

        MonsterSequence ms = monster.GetComponent<MonsterSequence>();
        if (ms != null)
        {
            if (deathT != null)
                ms.deathLine = deathT;

            // movement bebas, gak dikaitkan ke BPM
            ms.moveSpeed = Random.Range(0.8f, 1.2f); // contoh bebas, bisa disesuaikan

            // aktifkan sequence agar monster bersuara dan command berjalan
            ms.ActivateSequence();

            // ⚠ tidak perlu lagi assign currentMonster manual
            // PlayerInputHandler sekarang otomatis track semua monster via RegisterMonster()
        }
    }
}
