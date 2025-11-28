using UnityEngine;

public abstract class Abilities : ScriptableObject
{
    [Header("Ability Info")]
    public string abilityName;
    public float cooldownTime;
    public Animator animator;

    public abstract void Activate(GameObject user);

    public abstract void StopAnimation();
}
