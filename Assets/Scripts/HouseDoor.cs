using UnityEngine;
using UnityEngine.Audio;

public class HouseDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform _goingTo;
    [SerializeField] private AudioMixerGroup _ambientMixerGroup;
    [SerializeField] private AudioSource _source;
    [SerializeField] private float _targetedLowPass = 22000;
    [SerializeField] private bool _disableLight;
    [SerializeField] private bool _disableFog;
    private DayNightCycle dayNightCycle;
    private Rigidbody playerRigidbody;

    private void Start()
    {
        dayNightCycle = DayNightCycle.instance;
        playerRigidbody = PlayerInventory.instance.GetComponent<Rigidbody>();
    }

    public void Interact(GameObject sender)
    {
        //Teleport
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;

            playerRigidbody.MovePosition(_goingTo.position);
            playerRigidbody.MoveRotation(_goingTo.rotation);

            if (playerRigidbody.TryGetComponent<PlayerLook>(out var look))
            {
                Vector3 euler = _goingTo.rotation.eulerAngles;
                look.rotations.y = euler.y;
                look.rotations.x = 0f;
            }
        }
        else
        {
            // fallback if no Rigidbody
            playerRigidbody.transform.SetPositionAndRotation(_goingTo.position, _goingTo.rotation);
        }

        ChangeLights();
        _source.Play();
    }

    private void ChangeLights()
    {
        //Dem down the Ambient sounds
        _ambientMixerGroup.audioMixer.SetFloat("LowPassAmbience", _targetedLowPass);

        //bluh bluh bluh
        RenderSettings.fog = !_disableFog;
        dayNightCycle.SetIndoors(_disableFog);
        RenderSettings.ambientSkyColor = Color.black;
        RenderSettings.ambientGroundColor = Color.black;
        RenderSettings.ambientEquatorColor = Color.black;

        //Disable MainLight
        RenderSettings.sun.enabled = !_disableLight;
    }
}
