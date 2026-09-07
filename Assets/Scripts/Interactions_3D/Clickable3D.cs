using UnityEngine;
using UnityEngine.Events;

public class Clickable3D : MonoBehaviour
{
    public UnityEvent onLeftClick;
    public UnityEvent onRightClick;

    public void Click(int button)
    {
        if (button == 0) onLeftClick?.Invoke();
        else if (button == 1) onRightClick?.Invoke();
    }
}