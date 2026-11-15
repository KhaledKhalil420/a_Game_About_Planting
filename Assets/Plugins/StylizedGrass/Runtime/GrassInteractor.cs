using System.Collections.Generic;
using UnityEngine;

namespace StylizedGrass
{
    /// <summary>
    /// Put this on the player, enemies, props... anything that should bend / flatten grass.
    /// Tip for readability: on ENEMIES use Flatten = 1 and a radius a bit bigger than the body,
    /// so they always stand on cleared ground and can never be hidden by grass.
    /// </summary>
    [AddComponentMenu("Stylized Grass/Grass Interactor")]
    public class GrassInteractor : MonoBehaviour
    {
        public static readonly List<GrassInteractor> All = new List<GrassInteractor>();

        [Tooltip("Influence radius in meters.")]
        public float radius = 1.2f;

        [Range(0f, 1f), Tooltip("How much the grass is squashed down (1 = blades fully lie down, combed away from the center).")]
        public float flatten = 0.85f;

        [Range(0f, 0.95f), Tooltip("Fraction of the radius that is FULLY flattened. The rest is a soft falloff.")]
        public float core = 0.5f;

        [Tooltip("How far (meters) blade tips get pushed away from the center.")]
        public float push = 0.35f;

        [Tooltip("Max vertical distance (meters) between this object's pivot and the grass it affects. Raise it if your pivot is high above the ground.")]
        public float verticalRange = 3f;

        [Tooltip("Offset from the transform (use to put the center at the feet).")]
        public Vector3 offset = Vector3.zero;

        public Vector3 Position => transform.position + offset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.8f);
            Gizmos.DrawWireSphere(Position, radius);
        }
    }
}
