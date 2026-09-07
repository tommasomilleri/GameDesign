using UnityEngine;

public class CowRef : MonoBehaviour
{
    public int index;
    public Level1Manager manager;

    public void Click()
    {
        if (manager != null) manager.SelectCow(gameObject, index);
    }
}