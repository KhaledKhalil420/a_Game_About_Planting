using UnityEngine;
using DG.Tweening;

public class SpruceTree : Breakable
{
    [SerializeField] private AudioSource source;
    [SerializeField] private Renderer childTree;
    [SerializeField] private Item Pinecone;
    internal int coins = 5;

    private void Start()
    {
        childTree.material = new Material(childTree.material);

        if (isBreakable)
            return;

        Vector3 originalScale = transform.localScale;

        transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        transform.DOScale(originalScale, 250).OnComplete(() => isBreakable = true);
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

        PlayersPurse.GiveCoins(coins);
        
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

    private void OnDestroy()
    {
        if(Pinecone != null && enabled)
            Instantiate(Pinecone, transform.position + new Vector3(0, 1, 0), Quaternion.identity);
    }
}
