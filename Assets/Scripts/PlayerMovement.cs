using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        // ambil input horizontal saja (A/D atau panah kiri/kanan)
        float x = Input.GetAxis("Horizontal");

        // movement horizontal saja, y tetap sama, z tetap sama
        Vector3 move = new Vector3(x, 0, 0) * moveSpeed * Time.deltaTime;

        transform.Translate(move);
    }
}

