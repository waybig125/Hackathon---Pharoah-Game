using UnityEngine;

namespace TheAlchemistsCrypt.UI
{
    /// <summary>
    /// Self-healing utility to clear particle and missing script errors.
    /// </summary>
    public class MobileHUDFixer : MonoBehaviour
    {
        void Awake()
        {
            // 1. Purge Environment "Breeze" (Moving Boxes)
            var breeze = GameObject.Find("BreezeWeather");
            if (breeze) Destroy(breeze);
            
            // 2. Purge Competing HUDs
            var allCanvases = GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (var c in allCanvases)
            {
                string n = c.name.ToLower();
                if (n != "mobilehud_root" && (n.Contains("hud") || n.Contains("canvas") || n.Contains("joystick")))
                {
                    if (c.gameObject.name != "MobileHUD_Root") Destroy(c.gameObject);
                }
            }

            // 3. Fix Projectile Leak (Destroy all projectiles parented to root to clear lag)
            var allProps = GameObject.FindObjectsByType<TheAlchemistsCrypt.Weapons.Projectile>(FindObjectsInactive.Include);
            foreach (var p in allProps) {
                if (p.transform.parent == null) Destroy(p.gameObject);
            }

            // 4. Fix Particle Density & Velocity
            var particles = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include);
            foreach (var ps in particles)
            {
                var main = ps.main;
                main.maxParticles = 50; // Cap particles to save GPU

                var emis = ps.emission;
                emis.rateOverTime = 15; // Aggressively reduce density

                var vel = ps.velocityOverLifetime;
                if (vel.enabled)
                {
                    var x = vel.x; x.mode = ParticleSystemCurveMode.Constant; vel.x = x;
                    var y = vel.y; y.mode = ParticleSystemCurveMode.Constant; vel.y = y;
                    var z = vel.z; z.mode = ParticleSystemCurveMode.Constant; vel.z = z;
                }

                if (ps.name.Contains("Dust") || ps.name.Contains("Particle") || ps.name.Contains("Sand"))
                {
                    ps.transform.localScale = Vector3.one * 0.1f; 
                    main.startSize = new ParticleSystem.MinMaxCurve(main.startSize.constant * 0.1f);
                }
            }
            Debug.Log("MobileHUDFixer: Scene Purge & FPS Optimization Complete.");
        }
    }
}
