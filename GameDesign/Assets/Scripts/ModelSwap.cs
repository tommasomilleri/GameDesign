using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelSwap : MonoBehaviour
{
    public GameObject Fante;
    public GameObject Cavallo;
    public GameObject Re;

    public void ActivateModel(string characterType)
    {
        Fante.SetActive(characterType == "Fante");
        Cavallo.SetActive(characterType == "Cavallo");
        Re.SetActive(characterType == "Re");
    }
}


