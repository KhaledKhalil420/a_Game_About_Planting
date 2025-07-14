using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField] private int health = 100;

    public void Damage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            OnDestroyed();
        }
    }

    public virtual void OnDestroyed()
    {
        
    }
}
