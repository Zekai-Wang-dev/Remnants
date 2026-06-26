using UnityEngine;

public class EncounterTrigger : MonoBehaviour
{
    public string encounterId;
    public string areaId;

    private void OnValidate()
    {
        encounterId = GameIdUtility.Normalize(encounterId);
        areaId = GameIdUtility.Normalize(areaId);
    }
}
