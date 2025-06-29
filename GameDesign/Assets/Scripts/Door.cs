using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Costo e apertura porta")]
    public int goldCost = 20;
    public float openAngle = 90f;
    public float openSpeed = 2f;

    [Header("Oggetti da spostare")]
    public Transform camera;
    public Vector3 offsetMovimentoCamera = new Vector3(0, 3, 0);
    public float movimentoCameraSpeed = 2f;

    public Transform tetto;
    public Vector3 offsetMovimentoTetto = new Vector3(0, 3, 0);
    public float movimentoTettoSpeed = 2f;

    [Header("Giocatore 1")]
    public string player1Tag = "Player1";
    public KeyCode player1Key;

    [Header("Giocatore 2")]
    public string player2Tag = "Player2";
    public KeyCode player2Key;

    private bool isOpen = false;
    private bool isRotating = false;
    private Quaternion targetRotation;

    private bool spostaCamera = false;
    private Vector3 cameraStartPos;
    private Vector3 cameraTargetPos;

    private bool spostaTetto = false;
    private Vector3 tettoStartPos;
    private Vector3 tettoTargetPos;

    private bool player1Inside = false;
    private bool player2Inside = false;

    private PlayerHealth player1Ref;
    private PlayerHealth player2Ref;

    void Update()
    {
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

        if (spostaCamera && camera != null)
        {
            camera.position = Vector3.Lerp(camera.position, cameraTargetPos, Time.deltaTime * movimentoCameraSpeed);
            if (Vector3.Distance(camera.position, cameraTargetPos) < 0.05f)
            {
                camera.position = cameraTargetPos;
                spostaCamera = false;
                Debug.Log("Camera spostata completamente");
            }
        }

        if (spostaTetto && tetto != null)
        {
            tetto.position = Vector3.Lerp(tetto.position, tettoTargetPos, Time.deltaTime * movimentoTettoSpeed);
            if (Vector3.Distance(tetto.position, tettoTargetPos) < 0.05f)
            {
                tetto.position = tettoTargetPos;
                spostaTetto = false;
                Debug.Log("Tetto spostato completamente");
            }
        }
    }

    private bool TryOpenDoor(PlayerHealth player)
    {
        if (player != null && player.TrySpendGold(goldCost))
        {
            OpenDoor(spostaCamera: false); // apertura manuale, NO spostamento camera
            return true;
        }

        Debug.Log("Non abbastanza oro oppure player nullo");
        return false;
    }

    private void OpenDoor(bool spostaCamera)
    {
        if (isOpen) return;

        Debug.Log("Apertura porta avviata");
        isOpen = true;
        isRotating = true;
        targetRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0f, openAngle, 0f));

        if (tetto != null)
        {
            spostaTetto = true;
            tettoStartPos = tetto.position;
            tettoTargetPos = tettoStartPos + offsetMovimentoTetto;
            Debug.Log("Inizio movimento tetto");
        }

        if (spostaCamera && camera != null)
        {
            spostaCamera = true;
            cameraStartPos = camera.position;
            cameraTargetPos = cameraStartPos + offsetMovimentoCamera;
            Debug.Log("Inizio movimento camera");
        }
    }

    public void ForceOpen()
    {
        if (!isOpen)
        {
            Debug.Log("Apertura forzata ricevuta");
            OpenDoor(spostaCamera: true);
        }
    }

    // Metodo pubblico per spostare la camera sempre, anche se porta aperta
    public void MoveCamera()
    {
        if (camera == null)
        {
            Debug.LogWarning("Camera non assegnata!");
            return;
        }

        spostaCamera = true;
        cameraStartPos = camera.position;
        cameraTargetPos = cameraStartPos + offsetMovimentoCamera;
        Debug.Log("Movimento camera forzato");
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
