using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    [Header("References")]
    public BossShooter boss;
    public Image fillImage;

    void Update()
    {
        // Make sure boss and fill exist before updating
        if (boss == null || fillImage == null) return;

        // Calculate ratio of current HP
        float ratio = (float)boss.CurrentHP / boss.MaxHP;

        // Apply ratio to fill amount
        fillImage.fillAmount = ratio;
    }
}
