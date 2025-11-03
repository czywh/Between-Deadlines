using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [Header("References")]
    public BossShooter boss;
    public Image fillImage;

    void Update()
    {
        // ensure boss and fill exist before updating
        if (boss == null || fillImage == null) return;

        // calculate ratio of current HP
        float ratio = (float)boss.CurrentHP / boss.MaxHP;

        // apply ratio to health bar fill amount
        fillImage.fillAmount = ratio;
    }
}
