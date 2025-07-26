using UnityEngine;

public class DefenseStructure : MonoBehaviour
{
    public float attackRange = 5f;
    public float attackDamage = 10f;
    public float attackInterval = 1f;

    private float timer;

    [SerializeField]
    private Faction owner; // falls vorhanden wird dieser Owner genutzt

    private Building building;

    private void Awake()
    {
        building = GetComponent<Building>();
        if (building != null)
            owner = building.GetOwner();
    }

    private void Start()
    {
        // SetOwner() wird i.d.R. nach Awake aufgerufen
        if (owner == null && building != null)
            owner = building.GetOwner();
    }

    public void SetOwner(Faction faction)
    {
        owner = faction;
    }

    private void Update()
    {
        if (owner == null)
            return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            TryAttack();
            timer = attackInterval;
        }
    }

    private void TryAttack()
    {
        // Falls nach dem Spawn noch kein Besitzer gesetzt wurde
        if (owner == null && building != null)
            owner = building.GetOwner();

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var hit in hits)
        {
            Unit u = hit.GetComponent<Unit>();
            if (u == null) continue;

            if (owner != null && u.GetOwnerFaction() == owner)
                continue; // eigene Einheiten nicht angreifen

            Health hp = u.GetComponent<Health>();
            if (hp != null)
                hp.TakeDamage(attackDamage);
        }
    }
}
