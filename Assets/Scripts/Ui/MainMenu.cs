using System.IO;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public CanvasGroup group;

    public void NewGame()
    {
        CrossFader.StartFade(0.25f, 4, 0f, GetRandomTip(), () => SceneManager.LoadScene("Farmland"), true);
        group.interactable = false;
        group.blocksRaycasts = false;
        SaveFile file = new();
        string json = JsonUtility.ToJson(file, true);
        File.WriteAllText(Application.persistentDataPath + "/save.json", json);
        PlayerPrefs.DeleteAll();
    }

    public void LoadGame()
    {
        group.interactable = false;
        group.blocksRaycasts = false;
        CrossFader.StartFade(0.25f, 4, 0f, GetRandomTip(), () => SceneManager.LoadScene("Farmland"), true);
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
        string[] tips = new string[] { "Check The Key Maps For Controls!", "This is a tip", "Have a Good Time!", "Tip of the day: Tea is overrrated", "You can sell Crops in the bin", "You can reset trades by pressing the bell", "Made by a human (Probably)", "Stay hydrated!", "You need to water crops", "Cutting wood gives you coins", "You can plant cotton for some reason", "I forgot to add a tip", "Play stardew valley instead"};
        return tips[Random.Range(0, tips.Length)];
    }
}