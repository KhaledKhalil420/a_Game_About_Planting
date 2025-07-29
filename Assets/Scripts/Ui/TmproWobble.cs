using UnityEngine;
using TMPro;

public class TmproWobble : MonoBehaviour
{
    public TMP_Text text;
    private Mesh mesh;
    private Vector3[] vertices;
    public float speed;

    private void Update()
    {
        text.ForceMeshUpdate();
        mesh = text.mesh;
        vertices = mesh.vertices;

        for (int i = 0; i < text.textInfo.characterCount; i++)
        {
            TMP_CharacterInfo c = text.textInfo.characterInfo[i];
            int index = c.vertexIndex;

            Vector3 offset = Wobble(Time.time + i);
            try
            {
                vertices[index] += offset;
                vertices[index + 1] += offset;
                vertices[index + 2] += offset;
                vertices[index + 3] += offset;
            }
            catch
            {

            }
            
        }

        mesh.vertices = vertices;
        text.canvasRenderer.SetMesh(mesh);
    }

    Vector2 Wobble(float time)
    {
        return new Vector2(Mathf.Sin(time * 3.3f), Mathf.Cos(time * 2.5f)) * speed;
    }
}
