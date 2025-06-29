using TMPro;
using UnityEngine;

public class CardValueDisplay : MonoBehaviour
{
    public TextMeshPro valueText;

    public void SetValue(int val)
    {
        if (valueText == null)
            valueText = GetComponentInChildren<TextMeshPro>();
        if (valueText != null)
            valueText.text = val.ToString();
    }

    void LateUpdate()
    {
        //if (Camera.main != null)
            //valueText.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }
}

