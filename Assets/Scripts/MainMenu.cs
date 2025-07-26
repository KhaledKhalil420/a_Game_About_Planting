using System.IO;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public Animator fadeInCanvas;
    public CanvasGroup group;

    public void NewGame()
    {
        fadeInCanvas.Play("Fade");
        group.interactable = false;
        DOVirtual.DelayedCall(1, () => SceneManager.LoadScene("Farmland"), false);
        SaveFile file = new();
        string json = JsonUtility.ToJson(file, true);
        File.WriteAllText(Application.persistentDataPath + "/save.json", json);
        PlayerPrefs.DeleteAll();
    }

    public void LoadGame()
    {
        fadeInCanvas.Play("Fade");
        group.interactable = false;
        DOVirtual.DelayedCall(1, () => SceneManager.LoadScene("Farmland"), false);
    }

    public void OpenCredits()
    {

    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}