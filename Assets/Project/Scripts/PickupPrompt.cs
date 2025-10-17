using UnityEngine;
using UnityEngine.UI;

public class PickupPrompt : MonoBehaviour
{
    public GameObject promptUI;  //text object
    private bool isPlayerNearby = false;

    void Start()
    {
        // Hide the prompt at the start
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void Update()
    {
        // When the player is nearby and presses P
        if (isPlayerNearby && Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("Picked up item!");
            //destroy the item
            Destroy(gameObject);
        }
    }

    //When the player enters the trigger area
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            promptUI.SetActive(true);
        }
    }

    //When the player leaves the trigger area
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            promptUI.SetActive(false);
        }
    }
}