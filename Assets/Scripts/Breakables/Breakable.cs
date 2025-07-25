using UnityEngine;

public class Breakable : SaveableObject
{
    [Savable] public int health = 100;
    [Savable] private bool isKilled = false;
    [Savable, SerializeField] internal bool isBreakable = false;

    private void Start()
    {
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void Damage(int damage)
    {
        if (isKilled) return;

        health -= damage;

        if (health <= 0)
        {
            isKilled = true;
            OnDestroyed();
        }
    }

    public virtual void OnDestroyed()
    {

    }
}
