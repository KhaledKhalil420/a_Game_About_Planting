using UnityEngine;

public class Tool : Item
{
    protected Transform _camera;
    public Animator animator;

    public override void OnThrow()
    {
        animator.enabled = false;
    }

    public override void OnSelect()
    {
        animator.enabled = true;
    }
}
