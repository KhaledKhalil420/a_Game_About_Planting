using UnityEngine;
using DG.Tweening;

public class Tree : Breakable
{
    [SerializeField] private AudioSource source;
    [SerializeField] private Renderer childTree;

    private void Start()
    {
        childTree.material = new Material(childTree.material);
    }

    public override void OnDestroyed()
    {
        GetComponent<Collider>().enabled = false;
        BreakingSequence();
    }

    private void BreakingSequence()
    {
        Vector3 dir = -(PlayerInventory.instance.transform.position - transform.position).normalized;
        Vector3 axis = Vector3.Cross(Vector3.up, dir);
        Quaternion targetRot = Quaternion.AngleAxis(85f, axis);

        transform
            .DORotateQuaternion(targetRot, 4)
            .SetEase(Ease.OutBounce);

        source.Play();

        PlayersPurse.GiveCoins(5);
        Destroy(gameObject, 7f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Color color = childTree.material.GetColor("_Color");
            DOTween.To(() => color.a, a =>
            {
                color.a = a;
                childTree.material.SetColor("_Color", color);
            }, 33f / 255f, 1f);
        }
    }


    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Color color = childTree.material.GetColor("_Color");
            DOTween.To(() => color.a, a =>
            {
                color.a = a;
                childTree.material.SetColor("_Color", color);
            }, 1f, 1f);
        }
    }


}
