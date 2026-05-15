using UnityEngine;
using InfimaGames.LowPolyShooterPack;

public class PunchCombat : MonoBehaviour
{
    [SerializeField] private float range = 2.0f;
    [SerializeField] private float damage = 20f;
    [SerializeField] private LayerMask mask;
    
    private Weapon fistsWeapon;
    private Character playerCharacter;

    void Awake()
    {
        fistsWeapon = GetComponent<Weapon>();
        playerCharacter = GetComponentInParent<Character>();
    }

    void Update()
    {
    }
    
    public void Punch()
    {
        Debug.Log("Punching!");
        if (playerCharacter == null || playerCharacter.GetCameraWorld() == null) return;

        Ray ray = new Ray(playerCharacter.GetCameraWorld().transform.position, playerCharacter.GetCameraWorld().transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, mask))
        {
            Debug.Log($"Hit {hit.collider.name} with tag {hit.collider.tag}");
            
            // Handle LPSP objects by components instead of interfaces
            
            // Target
            var target = hit.collider.GetComponent<TargetScript>();
            if (target != null) target.isHit = true;
            
            // Explosive Barrel
            var barrel = hit.collider.GetComponent<ExplosiveBarrelScript>();
            if (barrel != null) barrel.explode = true;
            
            // Gas Tank
            var tank = hit.collider.GetComponent<GasTankScript>();
            if (tank != null) tank.isHit = true;

            // Apply physical force
            Rigidbody hitRb = hit.collider.GetComponent<Rigidbody>();
            if (hitRb != null)
            {
                hitRb.AddForceAtPosition(ray.direction * damage, hit.point, ForceMode.Impulse);
            }
        }
    }
}
