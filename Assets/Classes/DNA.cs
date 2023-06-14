using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DNA
{
    public bool isFemale;
    public float speed;
    public float sensoryDistance;

    public DNA CrossingOver(DNA father, DNA mother)
    {
        DNA child = new();
        child.isFemale = Random.Range(0, 2) == 0;

        if (Random.Range(0, 2) == 0)
            child.speed = MutateValue(father.speed);
        else
            child.speed = MutateValue(mother.speed);

        if (Random.Range(0, 2) == 0)
            child.sensoryDistance = MutateValue(father.sensoryDistance);
        else
            child.sensoryDistance = MutateValue(mother.sensoryDistance);

        return child;
    }

    private float MutateValue(float originalValue)
    {
        return originalValue + Random.Range(-0.2f, 0.2f);
    }
}
