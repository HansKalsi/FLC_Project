using UnityEngine;

public class GrassController : MonoBehaviour
{
    private float cutYPosition = 0.1f;
    private Color cutColour = Color.grey;

    void OnTriggerEnter(Collider other)
    {
        if (gameObject.CompareTag("CutGrass")) {
            // Grass already cut
            return;
        }
        if (other.CompareTag("Player")) {
            Debug.Log("Grass cut!");
            // Invert Y position then add cutYPosition (so the grass is only sticking out of the ground by the cutYPosition)
            float cutY = (transform.position.y * -1) + cutYPosition;
            transform.position = new Vector3(transform.position.x, cutY, transform.position.z);

            Renderer rend = GetComponent<Renderer>();
            rend.material.color = cutColour;

            gameObject.tag = "CutGrass";
        }
        if (other.CompareTag("Obstacle")) {
            Debug.Log("Obstacle hit!");
            Destroy(gameObject);
        }
    }
}
