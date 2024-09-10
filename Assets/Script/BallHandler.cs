using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewBehaviourScript : MonoBehaviour
{
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Rigidbody2D pivot;
    [SerializeField] private float detachDelay;
    [SerializeField] private float respawnDelay;
    [SerializeField] private float towerDelay;
    [SerializeField] private List<GameObject> pillars;

    private List<Vector3> pillarPositions = new List<Vector3>();
    private List<Quaternion> pillarRotations = new List<Quaternion>();

    private Rigidbody2D currentBallRigidbody;
    private SpringJoint2D currentBallSpringJoint;

    private Camera mainCamera;
    private bool isDragging;
    private bool isRespawning = false;
    private Vector3 ballStartPosition;
    private Quaternion ballStartRotation;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;

        foreach (var pillar in pillars)
        {
            pillarPositions.Add(pillar.transform.position);
            pillarRotations.Add(pillar.transform.rotation);
        }

        GameObject ballInstance = Instantiate(ballPrefab, pivot.position, Quaternion.identity);
        currentBallRigidbody = ballInstance.GetComponent<Rigidbody2D>();
        currentBallSpringJoint = ballInstance.GetComponent<SpringJoint2D>();
        currentBallSpringJoint.connectedBody = pivot;

        ballStartPosition = ballInstance.transform.position;
        ballStartRotation = ballInstance.transform.rotation;

        StartCoroutine(CheckTowerStatus());
    }

    // Update is called once per frame
    void Update()
    {
        if (currentBallRigidbody == null)
        {
            return;
        }

        if (!Touchscreen.current.primaryTouch.press.isPressed)
        {
            if (isDragging)
            {
                LaunchBall();
            }

            isDragging = false;

            return;
        }

        isDragging = true;

        currentBallRigidbody.isKinematic = true;

        Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(touchPosition);

        currentBallRigidbody.position = worldPosition;
    }

    private void ResetBall()
    {
        currentBallRigidbody.transform.position = ballStartPosition;
        currentBallRigidbody.transform.rotation = ballStartRotation;
        currentBallRigidbody.velocity = Vector2.zero;
        currentBallRigidbody.angularVelocity = 0f;

        currentBallSpringJoint.enabled = true;
        currentBallRigidbody.isKinematic = true;
    }

    private void LaunchBall()
    {
        currentBallRigidbody.isKinematic = false;

        Invoke(nameof(DetachBall), detachDelay);
    }

    private void DetachBall()
    {
        currentBallSpringJoint.enabled = false;

        Invoke(nameof(ResetBall), respawnDelay);
    }

    public void RespawnTower()
    {
        for (int i = 0; i < pillars.Count; i++)
        {
            pillars[i].transform.position = pillarPositions[i];
            pillars[i].transform.rotation = pillarRotations[i];

            Rigidbody2D pillarRb = pillars[i].GetComponent<Rigidbody2D>();
            if (pillarRb != null)
            {
                pillarRb.velocity = Vector2.zero;
                pillarRb.angularVelocity = 0f;
            }
        }

        isRespawning = false;
    }

    private bool AllPillarsMoved()
    {
        for (int i = 0; i < pillars.Count; i++)
        {
            if (Vector3.Distance(pillars[i].transform.position, pillarPositions[i]) < 0.1f)
            {
                return false;
            }
        }
        return true;
    }

    private IEnumerator CheckTowerStatus()
    {
        while (true)
        {
            if (!isRespawning && AllPillarsMoved())
            {
                isRespawning = true;
                yield return new WaitForSeconds(towerDelay);
                RespawnTower();
            }
            yield return null;
        }
    }
}
