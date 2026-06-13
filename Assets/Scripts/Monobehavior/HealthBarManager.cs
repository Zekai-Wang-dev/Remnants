using UnityEngine;
using UnityEngine.UI;

public class HealthBarManager : MonoBehaviour
{
    [Header("Required")]
    public Image healthBarFill;

    [Header("Follow Settings")]
    public Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

    private Character character;
    private Transform followTarget;
    private float maxHealth = 1f;

    public void Setup(Character characterToTrack, Transform targetToFollow)
    {
        character = characterToTrack;
        followTarget = targetToFollow;

        if (character == null || character.data == null)
        {
            Debug.LogWarning("HealthBarManager Setup failed: missing Character or CharacterData.");
            return;
        }

        maxHealth = Mathf.Max(1f, character.data.maxHealth);
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;

        UpdateHealthBar();
    }

    void LateUpdate()
    {
        if (character == null || followTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Camera.main.WorldToScreenPoint(followTarget.position + worldOffset);
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBarFill == null || character == null) return; 

        float fill = Mathf.Clamp01(character.health / maxHealth);
        healthBarFill.fillAmount = fill;

      

        Debug.Log($"Updated health bar for {character.name}: {character.health}/{maxHealth} ({fill * 100}%)");
    }

    public float map(float s, float a1, float a2, float b1, float b2)
    {
        return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
    }

}
