using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    void Awake()
    {
        List<(int, char)> cards2 = new List<(int, char)>();
        foreach ((int, char) card in cards)
        {
            cards2.Add(card);
        }
        /* for (int i = 0; i < 15; i++)
        {
            //Spawn a card
        } */
    }
}