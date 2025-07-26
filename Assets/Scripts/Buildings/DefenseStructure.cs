using UnityEngine;

public class DefenseStructure : MonoBehaviour
{
    public float attackRange = 5f;
    public float attackDamage = 10f;
    public float attackInterval = 1f;

    private float timer;
    private Faction owner;

    private void Awake()
    {
        Building b = GetComponent<Building>();
        if (b != null)
            owner = b.GetOwner();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            TryAttack();
            timer = attackInterval;
        }
    }

    private void TryAttack()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var hit in hits)
        {
            Unit u = hit.GetComponent<Unit>();
            if (u == null) continue;

            if (owner != null && u.GetOwnerFaction() == owner)
                continue;

            Health hp = u.GetComponent<Health>();
            if (hp != null)
                hp.TakeDamage(attackDamage);
        }
    }
}
