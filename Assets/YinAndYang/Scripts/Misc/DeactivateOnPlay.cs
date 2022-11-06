using UnityEngine;

public class DeactivateOnPlay : MonoBehaviour
{
    void Start()
    {
        gameObject.SetActive(false);
    }
}
