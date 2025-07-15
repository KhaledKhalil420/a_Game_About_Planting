using UnityEngine;

public class Tool : Item
{
    protected Transform _camera;
    public Animator animator;

    private void Start()
    {
        _camera = PlayerLook.mainCamera.transform;
    }

    public override void OnThrow()
    {
        animator.enabled = false;
    }

    public override void OnSelect()
    {
        animator.enabled = true;
    }
}
