using UnityEngine;

public class SelfRotate : MonoBehaviour
{
    public Vector3 rotationSpeed = new Vector3(0, 50f, 0); // Rotazione sull'asse Y

    void Update()
    {
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
