using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Plant : MonoBehaviour
{
    public bool isBeingEaten;

    private void Start()
    {
        Destroy(gameObject, 25);
    }

    IEnumerator Die()
    {
        yield return new WaitForSeconds(25);
        if (!isBeingEaten)
            Destroy(gameObject);
    }
}
