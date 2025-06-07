using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GenerateCards : MonoBehaviour
{
    //c = coppe, s = spade, d = denari, b = bastoni
    public List<(int, char/*, int conteggio*/)> cards = new List<(int, char)>
    {
        (1, 'c'), (2, 'c'), (3, 'c'), (4,'c'), (5, 'c'), (6, 'c'), (7, 'c'),
        (1, 's'), (2, 's'), (3, 's'), (4,'s'), (5, 's'), (6, 's'), (7, 's'),
        (1, 'b'), (2, 'b'), (3, 'b'), (4,'b'), (5, 'b'), (6, 'b'), (7, 'b'),
        (1, 'd'), (2, 'd'), (3, 'd'), (4,'d'), (5, 'd'), (6, 'd'), (7, 'd')
    };

    public GameObject card_to_copy;
    public TextMeshProUGUI remaining_cards;
    public Material coppe;
    public Material spade;
    public Material denari;
    public Material bastoni;
    private List<(int, char)> cards2 = new List<(int, char)>();

    void Awake()
    {
        remaining_cards.text = "";
        card_to_copy.SetActive(false);
        foreach ((int, char) card in cards)
        {
            cards2.Add(card);
        }
        for (int i = 0; i < 10; i++)
        {
            CreateCard(cards2);
        }
        foreach ((int, char) card in cards2)
        {
            remaining_cards.text += $"{card.Item1}{card.Item2} ";
        }
    }

    public void SpawnCard(GameObject c) //fa apparire la carta passata sul terreno di gioco
    {
        //da inserire il controllo sulle due fasi di gioco
        float x = UnityEngine.Random.Range(-20.000000f, 20.000000f);
        float z = UnityEngine.Random.Range(-2.000000f, 15.000000f);

        c.transform.position = new Vector3(x, (float)1, z);
        c.SetActive(true);
        return;
    }

    public void CreateCard(List<(int, char)> deck)
    {
        GameObject my_card = Instantiate(card_to_copy);
        my_card.SetActive(false);

        System.Random rnd = new System.Random();
        int n = rnd.Next(0, deck.Count);
        my_card.GetComponent<Empty_Card>().my_value = deck[n].Item1;
        switch (deck[n].Item2)
        {
            case 'c':
                my_card.GetComponent<Empty_Card>().my_suit = "coppe";
                my_card.GetComponent<MeshRenderer>().material = coppe;
                break;
            case 'd':
                my_card.GetComponent<Empty_Card>().my_suit = "denari";
                my_card.GetComponent<MeshRenderer>().material = denari;
                break;
            case 'b':
                my_card.GetComponent<Empty_Card>().my_suit = "bastoni";
                my_card.GetComponent<MeshRenderer>().material = bastoni;
                break;
            case 's':
                my_card.GetComponent<Empty_Card>().my_suit = "spade";
                my_card.GetComponent<MeshRenderer>().material = spade;
                break;
            default:
                Environment.Exit(0);
                break;
        }
        SpawnCard(my_card);
        deck.RemoveAt(n);
    }
    /*servono un metodo Spawn e un metodo che crea un oggetto e gli dà tutti gli attributi della carta*/

    void Update()
    {
        if (Empty_Card.active_cards < 10 && cards2.Count > 0)
        {
            CreateCard(cards2);
            remaining_cards.text = "";
            foreach ((int, char) card in cards2)
            {
                remaining_cards.text += $"{card.Item1}{card.Item2} ";
            }
        }
    }
}