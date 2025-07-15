using UnityEngine;

public class Breakable : MonoBehaviour
{
    [SerializeField] private int health = 100;
    private bool isKilled = false;

    public void Damage(int damage)
    {
        health -= damage;

        if (health <= 0 && !isKilled)
        {
            isKilled = true;
            OnDestroyed();
        }
    }

    public virtual void OnDestroyed()
    {
        
    }
}
