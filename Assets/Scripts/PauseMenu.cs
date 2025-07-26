using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu instance;
    public bool isPaused = false;

    [Header("UI Elements")]
    public Transform parent;

    [Header("References")]
    public PlayerLook playerLook;
    public PlayerInventory playerInventory;
    public AudioMixerGroup group;

    private void Awake()
    {
        instance = this;
        parent.gameObject.SetActive(true);
    }

    private void Start()
    {
        parent.gameObject.SetActive(false);
    }

    public void QuitToMenu()
    {
        ResumeGame();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        CrossFader.StartFade();

        DOVirtual.DelayedCall(0.15f, () =>
        {
            SceneManager.LoadScene("Main Menu");
        });
        
        SaveManager.instance.Save();
     }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Trigger();
        }
    }

    public void Trigger()
    {
        isPaused = !isPaused;
        if (isPaused)
            PauseGame();
        else
            ResumeGame();

        parent.gameObject.SetActive(isPaused);
    }

    public void PauseGame()
    {
        Time.timeScale = 0.00000001f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        playerLook.enabled = false;
        playerInventory.enabled = false;

        InputEnabled(false);

        group.audioMixer.DOSetFloat("LowPass", 4000.00f, 0.5f).SetUpdate(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        playerLook.enabled = true;
        playerInventory.enabled = true;

        InputEnabled(true);

        group.audioMixer.DOSetFloat("LowPass", 22000.00f, 0.5f).SetUpdate(true);
    }

    private void InputEnabled(bool enabled)
    {
        
    }
}
