using UnityEngine;

namespace TheAlchemistsCrypt.Environment
{
    public class AmbientHorrorSFX : MonoBehaviour
    {
        private AudioSource ambientSource;
        private AudioSource impactSource;

        private void Start()
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            impactSource = gameObject.AddComponent<AudioSource>();

            // Setup Ambient Rumble (using Tank Flame as a base)
            var clip = Resources.Load<AudioClip>("Infima Games/Low Poly Shooter Pack - Free Sample/Audio/SFX/Explosives/S_Tank_Flame");
            if (clip != null) {
                ambientSource.clip = clip;
                ambientSource.loop = true;
                ambientSource.volume = 0.8f; // Increased from 0.3f
                ambientSource.pitch = 0.4f; // Deep rumble
                ambientSource.spatialBlend = 0f; // 2D for global ambiance
                ambientSource.Play();
            }

            // Secondary eerie layer
            var secondSource = gameObject.AddComponent<AudioSource>();
            if (clip != null) {
                secondSource.clip = clip;
                secondSource.loop = true;
                secondSource.volume = 0.4f;
                secondSource.pitch = 0.15f; // Extremely deep vibration
                secondSource.spatialBlend = 0f;
                secondSource.Play();
            }

            InvokeRepeating("PlayRandomImpact", 5f, 15f); // More frequent
        }

        private void PlayRandomImpact()
        {
            var clip = Resources.Load<AudioClip>("Infima Games/Low Poly Shooter Pack - Free Sample/Audio/SFX/Impacts/S_Thud");
            if (clip != null && impactSource != null) {
                impactSource.pitch = Random.Range(0.3f, 0.6f);
                impactSource.volume = Random.Range(0.5f, 0.8f); // Louder impacts
                impactSource.spatialBlend = 0.5f; // Slight 3D feel
                impactSource.PlayOneShot(clip);
            }
        }
    }
}
