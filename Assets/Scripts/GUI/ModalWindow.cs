using UnityEngine;
using UnityEngine.UI;



namespace Ui
{
    /// <summary>
    /// The modal window object to show a dialog box if connect is clicked
    /// and host and port have wrong values.
    /// </summary>
    public class ModalWindow : MonoBehaviour
    {
        /// <summary>
        /// The okay button to dispose the created dialog box.
        /// </summary>
        public Button okButton;

        /// <summary>
        /// The modal window game object.
        /// </summary>
        public GameObject modalWindow;

        /// <summary>
        /// The audioclip plays if the button is pressed.
        /// </summary>
        public AudioClip buttonPress;

        /// <summary>
        /// Show the modal dialog and make the main menu inactive.
        /// </summary>
        public void ShowModalDialog()
        {
            modalWindow.SetActive(true);
            okButton.onClick.RemoveAllListeners();
            okButton.gameObject.SetActive(true);
        }

        /// <summary>
        /// Close the modal dialog if the okay button is pressed.
        /// </summary>
        public void ClosePanel()
        {
            okButton.GetComponent<AudioSource>().PlayOneShot(buttonPress);
            modalWindow.SetActive(false);
        }
    }
}
