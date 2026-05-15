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
        // We override the 'Fire' behavior if this is the active weapon
        // This is a bit of a hack since we can't easily change the Weapon class
    }
    
    // We will call this from an Animation Event or a custom hook
    public void Punch()
    {
        Debug.Log("Punching!");
        Ray ray = new Ray(playerCharacter.GetCameraWorld().transform.position, playerCharacter.GetCameraWorld().transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, mask))
        {
            Debug.Log($"Hit {hit.collider.name} with tag {hit.collider.tag}");
            
            // Handle Target
            if (hit.collider.CompareTag("Target"))
            {
                var target = hit.collider.GetComponent<TargetScript>();
                if (target != null) target.isHit = true;
            }
            // Handle Explosive Barrel
            else if (hit.collider.CompareTag("ExplosiveBarrel"))
            {
                var barrel = hit.collider.GetComponent<ExplosiveBarrelScript>();
                if (barrel != null) barrel.explode = true;
            }
            // Handle Gas Tank
            else if (hit.collider.CompareTag("GasTank"))
            {
                var tank = hit.collider.GetComponent<GasTankScript>();
                if (tank != null) tank.isHit = true;
            }

            // Apply physical force if it has a rigidbody
            Rigidbody hitRb = hit.collider.GetComponent<Rigidbody>();
            if (hitRb != null)
            {
                hitRb.AddForceAtPosition(ray.direction * damage, hit.point, ForceMode.Impulse);
            }
        }
    }
}
