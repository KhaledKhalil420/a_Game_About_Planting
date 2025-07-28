using System.IO;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public CanvasGroup group;

    public void NewGame()
    {
        CrossFader.StartFade(0.25f, 4, 0f, GetRandomTip(), () => SceneManager.LoadScene("Farmland"));
        group.interactable = false;
        SaveFile file = new();
        string json = JsonUtility.ToJson(file, true);
        File.WriteAllText(Application.persistentDataPath + "/save.json", json);
        PlayerPrefs.DeleteAll();
    }

    public void LoadGame()
    {
        group.interactable = false;
        CrossFader.StartFade(0.25f, 4, 0f, GetRandomTip(), () => SceneManager.LoadScene("Farmland"));
    }

    public void OpenCredits()
    {

    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }

    public string GetRandomTip()
    {
        string[] tips = new string[] { "Check The Key Maps For Controls!", "This is a tip", "Have a Good Time", "Tip of Day: Tea is overrated", "You can sell Crops in the bin", "You can reset trades by pressing the bell" };
        return tips[Random.Range(0, tips.Length)];
    }
}