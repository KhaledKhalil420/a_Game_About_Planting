using EZCameraShake;
using UnityEngine;

public class Axe : Tool
{
    [SerializeField] private float range = 2f, cooldown = 1f;
    [SerializeField] private int damage = 50;
    Transform cam;
    float nextUse;

    void Start() => cam = PlayerLook.mainCamera.transform;

    public override void OnUse()
    {
        if (Time.time < nextUse)
            return;
        
        //Set cooldown
        nextUse = Time.time + cooldown;

        //Play animation
        animator.SetTrigger("Trigger");
    }

    public void Chop()
    {
        if (!Physics.Raycast(cam.position, cam.forward, out var hit, range))
            return;

        if (hit.transform.TryGetComponent<SpruceTree>(out var tree))
        {
            //Camera shake
            CameraShaker.Instance.ShakeOnce(10, 2, 0.15f, 0.2f);

            ///Hit tree
            tree.Damage(damage);

            //Play Sound
            AudioManager.instance?.PlaySound("Axe_Hit", 1, 1.2f);
        }
    }
}