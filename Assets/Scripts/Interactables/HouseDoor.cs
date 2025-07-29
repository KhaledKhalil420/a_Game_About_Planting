using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening;

public class HouseDoor : MonoBehaviour, IInteractable
{
    //I regret adding the "_" but I'm balls deep now

    [SerializeField] Transform _goingTo;
    [SerializeField] AudioMixerGroup _ambientMixerGroup;
    [SerializeField] AudioSource _source;
    [SerializeField] float _targetedLowPass = 22000;
    [SerializeField] bool _disableLight, _disableFog, _onTrigger, _setIndoors;
    [SerializeField] GameObject _SpawnIn, _DisableIn;
    [SerializeField] Color _fogColor;

    DayNightCycle _dayNight;
    Rigidbody _playerRb;

    void Start()
    {
        _dayNight = DayNightCycle.instance;
        _playerRb = PlayerInventory.instance.GetComponent<Rigidbody>();
    }

    public void Interact(GameObject sender)
    {
        if (_playerRb == null) return;

        CrossFader.StartFade();

        DOVirtual.DelayedCall(0.15f, () =>
        {
            TeleportPlayer();
            ChangeLights();
            _source.Play();
        });
    }

    void OnTriggerEnter(Collider other)
    {
        if (!_onTrigger || !other.CompareTag("Player") || _playerRb == null) return;

        TeleportPlayer();
        
        if(_SpawnIn != null)
            _SpawnIn?.SetActive(true);

        if(_DisableIn != null)
            _DisableIn?.SetActive(false);


        CrossFader.StartFade();
        DOVirtual.DelayedCall(0.15f, () =>
        {
            TeleportPlayer();
            ChangeLights();
            _source.Play();
        });
    }

    void TeleportPlayer()
    {
        _playerRb.linearVelocity = Vector3.zero;
        _playerRb.angularVelocity = Vector3.zero;
        _playerRb.MovePosition(_goingTo.position);
        _playerRb.MoveRotation(_goingTo.localRotation);

        PlayerLook playerLook = _playerRb.GetComponent<PlayerMovement>().playerLook;

        var rot = _goingTo.localEulerAngles;
        playerLook.rotations = new Vector2(0f, rot.y);
    }

    void ChangeLights()
    {
        _ambientMixerGroup.audioMixer.SetFloat("LowPassAmbience", _targetedLowPass);

        RenderSettings.fogColor = _fogColor;
        RenderSettings.fog = !_disableFog;
        _dayNight.SetIndoors(_setIndoors);

        Color black = Color.black;
        RenderSettings.ambientSkyColor = black;
        RenderSettings.ambientGroundColor = black;
        RenderSettings.ambientEquatorColor = black;

        RenderSettings.sun.enabled = !_disableLight;
    }
}
