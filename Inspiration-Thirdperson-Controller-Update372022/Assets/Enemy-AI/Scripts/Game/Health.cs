using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour
{
    [Header("Health UI")]
    [SerializeField]
    private Image _healthUI;

    [HideInInspector] public float CurrrentHealth;

    private float _maxHealth;

    private void Start()
    {
        _maxHealth = CurrrentHealth;
        if (_healthUI != null)
            _healthUI.fillAmount = CurrrentHealth / _maxHealth;
    }

    public void ApplyDamage(float damage)
    {
        if (CurrrentHealth > 0)
        {
            CurrrentHealth -= damage;
            if (_healthUI != null)
                _healthUI.fillAmount = CurrrentHealth / _maxHealth;
        }
    }
}
