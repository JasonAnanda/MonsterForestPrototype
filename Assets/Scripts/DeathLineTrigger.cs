using UnityEngine;

public class DeathLineTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var monster = other.GetComponent<MonsterSequence>();
        if (monster != null)
        {
            Debug.Log("Monster melewati batas! Player kena penalti.");

            if (TrustMeterManager.Instance != null)
                TrustMeterManager.Instance.AddMiss();

            Destroy(other.gameObject);
        }
    }
}
