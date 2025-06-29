using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GenerateCards : MonoBehaviour
{
    public GameObject card_to_copy;
    public GameObject spawn; // oggetto con il collider su layer "Spawn"
    public TextMeshProUGUI remaining_cards;

    public Material coppe;
    public Material spade;
    public Material denari;
    public Material bastoni;

    public float distanzaMinimaTraCarte = 1.5f;
    public int maxTentativiPosizione = 30;

    private Bounds spawnBounds;
    private LayerMask spawnLayerMask;
    private List<Vector3> posizioniOccupate = new List<Vector3>();
    private List<(int, char)> cards = new List<(int, char)>
    {
        (1, 'c'), (2, 'c'), (3, 'c'), (4,'c'), (5, 'c'), (6, 'c'), (7, 'c'),
        (1, 's'), (2, 's'), (3, 's'), (4,'s'), (5, 's'), (6, 's'), (7, 's'),
        (1, 'b'), (2, 'b'), (3, 'b'), (4,'b'), (5, 'b'), (6, 'b'), (7, 'b'),
        (1, 'd'), (2, 'd'), (3, 'd'), (4,'d'), (5, 'd'), (6, 'd'), (7, 'd')
    };
    private List<(int, char)> cards2 = new List<(int, char)>();

    void Awake()
    {
        remaining_cards.text = "";
        card_to_copy.SetActive(false);

        if (spawn == null)
        {
            Debug.LogError("Spawn non assegnato!");
            return;
        }

        Collider col = spawn.GetComponent<Collider>();
        if (col != null)
        {
            spawnBounds = col.bounds;
        }
        else
        {
            Debug.LogError("Il GameObject 'spawn' deve avere un Collider.");
            return;
        }

        spawnLayerMask = LayerMask.GetMask("Spawn");

        foreach ((int, char) card in cards)
        {
            cards2.Add(card);
        }

        for (int i = 0; i < 10; i++)
        {
            CreateCard(cards2);
        }

        UpdateRemainingText();
    }

    void Update()
    {
        if (Empty_Card.active_cards < 10 && cards2.Count > 0)
        {
            CreateCard(cards2);
            UpdateRemainingText();
        }
    }

    public void CreateCard(List<(int, char)> deck)
    {
        GameObject my_card = Instantiate(card_to_copy);
        my_card.SetActive(false);

        System.Random rnd = new System.Random();
        int n = rnd.Next(0, deck.Count);
        int value = deck[n].Item1;
        char suit = deck[n].Item2;

        var cardScript = my_card.GetComponent<Empty_Card>();
        cardScript.my_value = value;

        switch (suit)
        {
            case 'c':
                cardScript.my_suit = "coppe";
                my_card.GetComponent<MeshRenderer>().material = coppe;
                break;
            case 'd':
                cardScript.my_suit = "denari";
                my_card.GetComponent<MeshRenderer>().material = denari;
                break;
            case 'b':
                cardScript.my_suit = "bastoni";
                my_card.GetComponent<MeshRenderer>().material = bastoni;
                break;
            case 's':
                cardScript.my_suit = "spade";
                my_card.GetComponent<MeshRenderer>().material = spade;
                break;
        }

        var valueDisplay = my_card.GetComponentInChildren<CardValueDisplay>();
        if (valueDisplay != null)
            valueDisplay.SetValue(value);

        SpawnCard(my_card);
        deck.RemoveAt(n);
    }

    public void SpawnCard(GameObject c)
    {
        int tentativi = 0;
        Vector3 posizioneValida = Vector3.zero;
        bool trovata = false;

        while (tentativi < maxTentativiPosizione && !trovata)
        {
            float x = UnityEngine.Random.Range(spawnBounds.min.x, spawnBounds.max.x);
            float z = UnityEngine.Random.Range(spawnBounds.min.z, spawnBounds.max.z);
            Vector3 puntoRay = new Vector3(x, spawnBounds.max.y + 5f, z);

            if (Physics.Raycast(puntoRay, Vector3.down, out RaycastHit hit, 20f, spawnLayerMask))
            {
                if (hit.collider.gameObject == spawn)
                {
                    float altezzaOffset = 3f; // regola a piacere
                    Vector3 posizioneTest = hit.point + Vector3.up * altezzaOffset;
                    if (!posizioniOccupate.Exists(p => Vector3.Distance(p, posizioneTest) < distanzaMinimaTraCarte))
                    {
                        posizioneValida = posizioneTest;
                        trovata = true;
                    }
                }
            }

            tentativi++;
        }

        if (trovata)
        {
            c.transform.position = posizioneValida;
            posizioniOccupate.Add(posizioneValida);
            c.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Posizione di spawn non trovata.");
        }
    }

    private void UpdateRemainingText()
    {
        remaining_cards.text = "";
        foreach ((int, char) card in cards2)
        {
            remaining_cards.text += $"{card.Item1}{card.Item2} ";
        }
    }
}
