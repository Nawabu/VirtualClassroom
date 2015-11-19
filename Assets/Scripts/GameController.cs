using UnityEngine;
using DisplayApplication;
using Network;



namespace Domain
{
    /// <summary>
    /// This class acts like a mediator between the client and the displays.
    /// It controls all basic functions of the game.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        /// <summary>
        /// All displays added to a list.
        /// </summary>
        private GameObject[] gameObjects;

        /// <summary>
        /// The client object to send messages.
        /// </summary>
	    private GameObject clientObject;

        /// <summary>
        /// The client script component.
        /// </summary>
	    private Client client;


        // Unity function: Use this for initialization.
        void Start()
        {
            gameObjects = GameObject.FindGameObjectsWithTag("Display");

            if (gameObjects != null)
            {
                /*
                 * This is the game object called clientObject which is located in the 
                 * main menu.
                 */
                clientObject = GameObject.Find("ClientObject");
                if (clientObject != null)
                {
                    client = clientObject.GetComponent<Client>();
                }
                else
                {
                    Debug.LogWarning("Client object does not exist.");
                }
            }

            else
            {
                Debug.LogWarning("There are no displays available with the tag 'Display'."
                                + " Please add this name tag to all displays.");
            }
        }


        /// <summary>
        /// Set the file to the corresponding display.
        /// </summary>
        /// <param name="number">The display with the specified ID which gets the file.</param>
        /// <param name="filePath">The file path which is located in the system directory.</param>
        /// <param name="tempDirectoryPath">The temp directory path in order to create a new folder if it is a PDF file.</param>
        public void SetFileToDisplay(int number, string filePath, string tempDirectoryPath)
        {
            foreach (GameObject gameObj in gameObjects)
            {
                // Set the file to the display with the specified display ID
                if (gameObj.GetComponent<DisplayController>().displayId == number)
                {
                    gameObj.GetComponent<DisplayController>().Refresh(filePath, tempDirectoryPath);
                }
            }
        }


        /// <summary>
        /// The user input which is made on a display is send to the client.
        /// </summary>
        /// <param name="displayId">The identification number of the diplay.</param>
        /// <param name="message">The message send from the corresponding display.</param>
	    public void SetUserInputToClient(int displayId, string message)
        {
            Debug.Log("Set message to client: " + "display ID: " + displayId + " Message: " + message);
            client.SendUserInputToServer(displayId, message);
        }

        /// <summary>
        /// The user input is received from client to refresh the input of the
        /// display with the corresponding display ID to all clients.
        /// </summary>
        /// <param name="displayId">The display ID.</param>
        /// <param name="message">The message received from server.</param>
	    public void SetUserInputToAllDisplays(int displayId, string message)
        {
            foreach (GameObject gameObj in gameObjects)
            {
                if (message.Equals("pauseVideo") || message.Equals("playVideo"))
                {
                    gameObj.GetComponent<DisplayController>().SetUserInputForPlayAndPause(displayId);
                }
                else if (message.Equals("stopVideo"))
                {
                    gameObj.GetComponent<DisplayController>().SetUserInputForStop(displayId);
                }
                else if (message.Equals("nextSlide"))
                {
                    gameObj.GetComponent<DisplayController>().SetNextPageForPdfFile(displayId);
                }
                else if (message.Equals("previousSlide"))
                {
                    gameObj.GetComponent<DisplayController>().SetPreviousPageForPdfFile(displayId);
                }
            }
        }
    }
}