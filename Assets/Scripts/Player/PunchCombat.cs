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
            Debug.Log($"Hit {hit.collider.name}");
            // Handle damage to barrels or enemies
            var damageable = hit.collider.GetComponentInParent<InfimaGames.LowPolyShooterPack.Interface.ITarget>();
            // Note: The LPSP has a Target component.
        }
    }
}
