using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace TheAlchemistsCrypt.Gameplay
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance;

        private ObjectPool<ParticleSystem> explosionPool;
        private ObjectPool<ParticleSystem> shatterPool;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            InitializePools();
        }

        private void InitializePools()
        {
            explosionPool = new ObjectPool<ParticleSystem>(
                createFunc: () => CreateExplosionVFX(new Color(0.2f, 0.9f, 0.2f)), // Acid Green
                actionOnGet: (ps) => ps.gameObject.SetActive(true),
                actionOnRelease: (ps) => ps.gameObject.SetActive(false),
                actionOnDestroy: (ps) => Destroy(ps.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 30
            );

            shatterPool = new ObjectPool<ParticleSystem>(
                createFunc: () => CreateExplosionVFX(new Color(0.15f, 0.6f, 1.0f)), // Ice Blue
                actionOnGet: (ps) => ps.gameObject.SetActive(true),
                actionOnRelease: (ps) => ps.gameObject.SetActive(false),
                actionOnDestroy: (ps) => Destroy(ps.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: 30
            );
        }

        private ParticleSystem CreateExplosionVFX(Color color)
        {
            GameObject go = new GameObject("VFX_Explosion");
            go.transform.SetParent(transform);
            
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startColor = color;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.Callback; // Triggers OnParticleSystemStopped

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 30, 50) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            // PERFORMANCE: Use unlit material and instancing
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
            mat.SetColor("_BaseColor", color);
            renderer.material = mat;
            renderer.enableGPUInstancing = true;

            var returnScript = go.AddComponent<VFXReturnToPool>();
            returnScript.pool = explosionPool;

            return ps;
        }

        public void PlayAcidExplosion(Vector3 position)
        {
            var ps = explosionPool.Get();
            ps.transform.position = position;
            ps.Play();
        }

        public void PlayShatterExplosion(Vector3 position)
        {
            var ps = shatterPool.Get();
            ps.transform.position = position;
            ps.Play();
        }
    }

    public class VFXReturnToPool : MonoBehaviour
    {
        public IObjectPool<ParticleSystem> pool;
        private ParticleSystem ps;

        private void Awake()
        {
            ps = GetComponent<ParticleSystem>();
        }

        private void OnParticleSystemStopped()
        {
            if (pool != null) pool.Release(ps);
        }
    }
}
