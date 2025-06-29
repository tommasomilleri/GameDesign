using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Costo e apertura porta")]
    public int goldCost = 20;
    public float openAngle = 90f;
    public float openSpeed = 2f;

    [Header("Oggetto da spostare")]
    public Transform oggettoDaSpostare;
    public Vector3 offsetMovimentoOggetto = new Vector3(0, 3, 0);
    public float movimentoOggettoSpeed = 2f;

    [Header("Giocatore 1")]
    public string player1Tag = "Player1";
    public KeyCode player1Key;

    [Header("Giocatore 2")]
    public string player2Tag = "Player2";
    public KeyCode player2Key;

    private bool isOpen = false;
    private bool isRotating = false;
    private Quaternion targetRotation;

    private bool spostaOggetto = false;
    private Vector3 oggettoStartPos;
    private Vector3 oggettoTargetPos;

    private bool player1Inside = false;
    private bool player2Inside = false;

    private PlayerHealth player1Ref;
    private PlayerHealth player2Ref;

    void Update()
    {
        // 🎮 INPUT solo se la porta non è già aperta o in movimento
        if (!isOpen && !isRotating)
        {
            if (player1Inside && Input.GetKeyDown(player1Key))
            {
                Debug.Log("Giocatore 1 ha premuto il tasto");
                if (TryOpenDoor(player1Ref)) return;
            }

            if (player2Inside && Input.GetKeyDown(player2Key))
            {
                Debug.Log("Giocatore 2 ha premuto il tasto");
                if (TryOpenDoor(player2Ref)) return;
            }
        }

        // 🚪 Rotazione della porta
        if (isRotating)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * openSpeed);
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.5f)
            {
                transform.rotation = targetRotation;
                isRotating = false;
                Debug.Log("La porta ha terminato la rotazione");
            }
        }

        // 🔁 Movimento oggetto extra
        if (spostaOggetto && oggettoDaSpostare != null)
        {
            oggettoDaSpostare.position = Vector3.Lerp(oggettoDaSpostare.position, oggettoTargetPos, Time.deltaTime * movimentoOggettoSpeed);
            if (Vector3.Distance(oggettoDaSpostare.position, oggettoTargetPos) < 0.05f)
            {
                oggettoDaSpostare.position = oggettoTargetPos;
                spostaOggetto = false;
                Debug.Log("Oggetto extra spostato completamente");
            }
        }
    }

    private bool TryOpenDoor(PlayerHealth player)
    {
        if (player != null && player.TrySpendGold(goldCost))
        {
            OpenDoor();
            return true;
        }

        Debug.Log("Non abbastanza oro oppure player nullo");
        return false;
    }

    void OpenDoor()
    {
        Debug.Log("Apertura porta avviata");
        isOpen = true;
        isRotating = true;
        targetRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0f, openAngle, 0f));

        if (oggettoDaSpostare != null)
        {
            spostaOggetto = true;
            oggettoStartPos = oggettoDaSpostare.position;
            oggettoTargetPos = oggettoStartPos + offsetMovimentoOggetto;
            Debug.Log("Inizio movimento oggetto extra");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(player1Tag))
        {
            player1Inside = true;
            player1Ref = other.GetComponent<PlayerHealth>();
            Debug.Log("Player1 è entrato nel trigger");
        }
        else if (other.CompareTag(player2Tag))
        {
            player2Inside = true;
            player2Ref = other.GetComponent<PlayerHealth>();
            Debug.Log("Player2 è entrato nel trigger");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(player1Tag))
        {
            player1Inside = false;
            player1Ref = null;
            Debug.Log("Player1 è uscito dal trigger");
        }
        else if (other.CompareTag(player2Tag))
        {
            player2Inside = false;
            player2Ref = null;
            Debug.Log("Player2 è uscito dal trigger");
        }
    }
}


