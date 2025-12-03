using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public MonsterSequence activeMonster; // assign otomatis oleh monster

    void Update()
    {
        if (activeMonster == null)
        {
            Debug.Log("No active monster assigned!");
            return;
        }

        if (Input.GetKeyDown(KeyCode.A)) activeMonster.HandlePlayerInput("A");
        if (Input.GetKeyDown(KeyCode.S)) activeMonster.HandlePlayerInput("S");
        if (Input.GetKeyDown(KeyCode.J)) activeMonster.HandlePlayerInput("J");
        if (Input.GetKeyDown(KeyCode.K)) activeMonster.HandlePlayerInput("K");
        if (Input.GetKeyDown(KeyCode.Space)) activeMonster.HandlePlayerInput("ÅE");
    }
}
