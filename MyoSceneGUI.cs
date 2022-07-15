using UnityEngine;
using System.Collections;

// Draw simple instructions for sample scene.
// Check to see if a Myo armband is paired.
public class MyoSceneGUI : MonoBehaviour
{
    // Myo game object to connect with.
    // This object must have a ThalmicMyo script attached.
    public GameObject myo = null;
    public GameManager gameManager;
    // Draw some basic instructions.
    
    void OnGUI()
    {
        GUI.skin.label.fontSize = 20;
        if (gameManager.getMyoMode())
        {
            ThalmicHub hub = ThalmicHub.instance;

            // Access the ThalmicMyo script attached to the Myo object.
            ThalmicMyo thalmicMyo = myo.GetComponent<ThalmicMyo>();

            if (!hub.hubInitialized)
            {
                GUI.Label(new Rect(12, 8, Screen.width, Screen.height),
                    "Cannot contact Myo Connect. Is Myo Connect running?\n" +
                    "Press Q to try again."
                );
            }
            else if (!thalmicMyo.isPaired)
            {
                GUI.Label(new Rect(12, 8, Screen.width, Screen.height),
                    "No Myo currently paired."
                );
            }
            else if (!thalmicMyo.armSynced)
            {
                GUI.Label(new Rect(12, 8, Screen.width, Screen.height),
                    "Perform the Sync Gesture."
                );
            }
            else
            {
                GUI.Label(new Rect(12, 8, Screen.width, Screen.height),
                    "Move arm: change car's direction.\n" +
                    "Fist: activate brakes. Repeat for stronger brakes.\n" +
                    "Double tap: accelerate from braking mode.\n"

                );
            }
        }
        else {
            GUI.Label(new Rect(12, 8, Screen.width, Screen.height),
                    "Horizontal keys: change car's direction.\n" +
                    "Spacebar: activate brakes. Repeat for stronger brakes.\n" +
                    "Shift: accelerate from braking mode.\n"

                );
        }
    }
    void Update ()
    {
        if (gameManager.getMyoMode())
        {
            ThalmicHub hub = ThalmicHub.instance;

            if (Input.GetKeyDown("q"))
            {
                hub.ResetHub();
            }
        }
    }
}
