using UnityEngine;
using UnityEngine.UI;


namespace Ui
{
    /// <summary>
    /// This class represents the last scene if the client is disconnected from server.
    /// It contains an exit button to close the application.
    /// </summary>
    public class DialogBoxMenu : MonoBehaviour
    {

        /// <summary>
        /// The exit button.
        /// </summary>
        public Button exit;

        /// <summary>
        /// The sound which is played if the exit button is pressed.
        /// </summary>
        public AudioClip buttonPress;

        /// <summary>
        /// Exits the application if the exit button is pressed.
        /// </summary>
        public void ExitApplication()
        {
            exit.GetComponent<AudioSource>().PlayOneShot(buttonPress);
            Application.Quit();
        }
    }
}
