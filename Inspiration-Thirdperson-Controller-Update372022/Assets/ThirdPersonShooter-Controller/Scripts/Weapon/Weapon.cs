using UnityEngine;
using UnityEngine.UI;
using WeirdBrothers;

public class Weapon : MonoBehaviour,IItemName,IItemImage
{
    [Header("Weawpon Data")]
    [Space]
    public WeaponData data;

    [Space]
    public RuntimeAnimatorController animator;

    [Space]
    public Transform leftHandRef;

    [Space]
    public Transform firePoint;
    public ParticleSystem muzzelFlash;

    [Space]
    public Transform mag;
    public Transform magRef;

    [Space]
    public Transform spawnCaseTransform;

    public int totalAmmo;    

    [HideInInspector] public int currentAmmo = 0;
    [HideInInspector] public float nextFire;

    public Sprite GetItemImage()
    {
        return data.WeaponImage;
    }

    public string GetItemName()
    {
        return data.WeaponName;
    }

    public void OnCaseOut()
    {
        GameObject ejectedCase = Instantiate(data.BulletCase, spawnCaseTransform.position, spawnCaseTransform.rotation);
        Rigidbody caseRigidbody = ejectedCase.GetComponent<Rigidbody>();
        caseRigidbody.velocity = spawnCaseTransform.TransformDirection(-Vector3.left * data.BulletEjectingSpeed);
        caseRigidbody.AddTorque(Random.Range(-0.5f, 0.5f), Random.Range(0.2f, 0.3f), Random.Range(-0.5f, 0.5f));
        caseRigidbody.AddForce(0, Random.Range(2f, 4f), 0, ForceMode.Impulse);

        Destroy(ejectedCase, 10f);
    }
}
