using UnityEngine;

public class PortalTeleport : MonoBehaviour
{
    public Transform player;
    public Transform outPortal;

    private bool isPlayerOverlapping = false;

    private void Update()
    {
        if (isPlayerOverlapping)
        {
            Vector3 portalToPlayer = player.position - transform.position;
            float dotProduct = Vector3.Dot(portalToPlayer, transform.up);
            Debug.Log("dot product is: " +  dotProduct);

            //player moved across
            if(dotProduct < 0f)
            {
                Debug.Log("TELEPORTING PLAYER!");

                CharacterController controller = player.GetComponent<CharacterController>();
                FPSMovement fpsMovement = player.GetComponent<FPSMovement>();

                // Portal math for proper positioning and rotation
                var inTransform = transform;
                var outTransform = outPortal;
                var halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

                Vector3 relativePos = inTransform.InverseTransformPoint(player.position);
                relativePos = halfTurn * relativePos;

                Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * player.rotation;
                relativeRot = halfTurn * relativeRot;

                controller.enabled = false;
                player.position = outTransform.TransformPoint(relativePos);
                player.rotation = outTransform.rotation * relativeRot;

                // Update both yaw values for instant rotation
                Vector3 newEulerAngles = player.rotation.eulerAngles;
                fpsMovement.yaw = newEulerAngles.y;
                fpsMovement.smoothYaw = newEulerAngles.y; // Now this will work!

                controller.enabled = true;

                Debug.Log($"Teleported to: {player.position}");
                Debug.Log($"New yaw: {fpsMovement.yaw}");
            }
        }
    }


    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log(collision.gameObject.tag);
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerOverlapping = true;
            Debug.Log("Player collided");
        }
    }

    private void OnTriggerExit(Collider collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            isPlayerOverlapping = false;
    }
}
