using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Predator : Creature 
{
    protected override void Awake()
    {
        base.Awake();
        foodType = "Player";
    }
}
