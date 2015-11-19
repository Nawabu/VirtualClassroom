using UnityEngine;
using UnityEngine.UI;
using Network;


namespace Ui
{
    /// <summary>
    /// The UI menu for the first connection window.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {

        /// <summary>
        /// The host to connect to server.
        /// </summary>
        private string host;

        /// <summary>
        /// The port to connect to server.
        /// </summary>
        private int port;

        /// <summary>
        /// The input field for host.
        /// </summary>
        public InputField inputFieldHost;

        /// <summary>
        /// The input field for port.
        /// </summary>
        public InputField inputFieldPort;

        /// <summary>
        /// The button to connect and forward to the main scene.
        /// </summary>
	    public Button connect;

        /// <summary>
        /// The button to exit the application.
        /// </summary>
	    public Button exit;

        /// <summary>
        /// The client script to give the host and port that is set by the user.
        /// </summary>
        public Client client;

        /// <summary>
        /// The audioclip plays if the button is pressed.
        /// </summary>
        public AudioClip buttonPress;


        /// <summary>
        /// Starts the application. This method is called if the connect button is pressed.
        /// </summary>
        public void StartApplication()
        {
            SetInputByUser();
            connect.GetComponent<AudioSource>().PlayOneShot(buttonPress);

            if (inputFieldHost.text.Length > 0 && inputFieldPort.text.Length > 0)
            {
                client.StartTCPConnection(host, port);

                if (client.IsClientConnected)
                {
                    Application.LoadLevel("MainScene");
                }
            }
            else
            {
                Debug.Log("Please enter a valid host and port.");
            }
        }

        /// <summary>
        /// Exits the application. This method is called if the exit button is pressed.
        /// </summary>
        public void ExitApplication()
        {
            exit.GetComponent<AudioSource>().PlayOneShot(buttonPress);
            Application.Quit();
        }

        /// <summary>
        /// The host and port input which is set by the user.
        /// </summary>
        public void SetInputByUser()
        {
            host = inputFieldHost.text;

            /** 
             * Try to parse the port typed in by the user as an integer
             * if it not possible because of a not valid format, then the
             * else statement will be executed.
             */
            if (int.TryParse(inputFieldPort.text, out port))
            {
                Debug.Log("Port is valid.");
            }
            else
            {
                Debug.Log("This entry for port is not valid. Please enter a number.");
            }
        }
    }
}
