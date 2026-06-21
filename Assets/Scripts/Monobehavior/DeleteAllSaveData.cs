using UnityEngine;
using UnityEngine.UI;

public class DeleteAllSaveData : MonoBehaviour
{

    public Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(DeleteSaveData);
    }

    public void DeleteSaveData()
    {

        GameSaveManager.Instance.DeleteAllSavedData();

    }

    
}
