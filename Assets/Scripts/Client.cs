using FileData;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using Ui;
using Domain;



namespace Network
{
    /// <summary>
    /// This is the client class to start a connection to the server and
    /// handle all files that are received to allocate them to the game controller.
    /// </summary>
    public class Client : MonoBehaviour
    {
        /// <summary>
        /// Network stream to allow sending and receiving data.
        /// </summary>
	    private NetworkStream networkStream;

        /// <summary>
        /// The object, the game controller script is attached to.
        /// </summary>
        private GameObject gameControllerObject;

        /// <summary>
        /// Game controller script component.
        /// </summary>
	    private GameController gameController;

        /// <summary>
        /// The path to the created temp folder inside the system directory temp folder.
        /// </summary>
	    private string tempDirectoryPath;

        /// <summary>
        /// The type of the message received from server.
        /// </summary>
        private string type = "";

        /// <summary>
        /// The client thread to listen to multiple messages.
        /// </summary>
        private Thread clientThread;

        /// <summary>
        /// The pair of the display ID and the corresponding file path.
        /// </summary>
	    private KeyValuePair<int, string> displayIdFilePath;

        /// <summary>
        /// The received messages from server are stored in a queue.
        /// </summary>
	    private Queue<KeyValuePair<int, string>> inboxMessages;

        /// <summary>
        /// Modal dialog object. 
        /// </summary>
        private GameObject modalDialogObject;

        /// <summary>
        /// Modal dialog script component.
        /// </summary>
        private ModalWindow modalDialog;

        /// <summary>
        /// Check if client is connected to server.
        /// </summary>
	    private bool isConnected = false;

        /// <summary>
        /// Check if client is disconnected from server.
        /// </summary>
        private bool isDisconnected = false;


        // Unity function: Use this for initialization.
        void Start()
	    {
		    SetPathToSystemDirectory();
		    displayIdFilePath = new KeyValuePair<int, string>();
		    inboxMessages = new Queue<KeyValuePair<int, string>>();
        }
	

        // Unity function: Initialize before start method is called.
	    void Awake()
	    {
		    DontDestroyOnLoad(this);
        }
	

        /// <summary>
        /// The TCP connection between client and server will be started.
        /// </summary>
        /// <param name="host">The host from server.</param>
        /// <param name="port">The port from server.</param>
	    public void StartTCPConnection(string host, int port)
	    {
		    try
		    {
			    TcpClient client = new TcpClient();
			    client.Connect(host, port);
                networkStream = client.GetStream();
                IsClientConnected = true;
                Debug.Log("Connected to server. \n Host " + host + " and " + "Port: " + port);

                // Check if path to OS temp folder is already available, otherwise create a new one.
                ResetSystemDirectory();

                clientThread = new Thread(ListenToMessagesFromServer);
                clientThread.Start();
            }
		    catch(Exception e)
		    {
                modalDialogObject = GameObject.Find("MainMenuManager");
                if(modalDialogObject != null)
                {
                    modalDialog = modalDialogObject.GetComponent<ModalWindow>();
                    modalDialog.ShowModalDialog();
                }
                else
                {
                    Debug.LogWarning("Modal dialog object does not exist.");
                }
                Debug.LogError("Unable to connect with server. Error: " + e);
		    } 
	    }


        // Unity function: Update is called once per frame.
        void Update()
	    {
            lock(inboxMessages)
		    {
			    while(inboxMessages.Count > 0)
			    {
				    displayIdFilePath = inboxMessages.Dequeue();
                    if(type.Equals("STR"))
                    {
                        Debug.Log("STR message accepted from server.");
                        SetUserInputToGameController(displayIdFilePath.Key, displayIdFilePath.Value);
                    }

                    else if(type.Equals("OBJ"))
                    {
                        Debug.Log("OBJ message accepted from server.");
                        SetFileToGameController(displayIdFilePath.Key, displayIdFilePath.Value);
                    }

                    else
                    {
                        Debug.LogWarning("There is no message received that contains either 'STR' or 'OBJ'");
                    }
			    }
		    }

            if(isDisconnected)
            {
                clientThread.Abort();
                Destroy(this);
                // Destroy this scene and load a new one.
                Application.LoadLevel("DialogBox");
            }
        }
	

        /// <summary>
        /// Listen to messages from server. 
        /// The client thread is started here.
        /// </summary>
	    public void ListenToMessagesFromServer()
	    {
            try
            {
                while(IsClientConnected)
		        {
                    Debug.Log("Client starts listen to messages from server.");
                    ReceiveMessages();
		        }
            }
            catch(Exception e)
            {
                Debug.Log("Receive message failed: " + e);
                isDisconnected = true;
                CloseConnection();
            }
            finally
            {
                if(!networkStream.DataAvailable)
                {
                    Debug.Log("Client is disconnected");
                } 
            }
        }
	

        /// <summary>
        /// Receive messages which are sent from the server.
        /// It can be either a string message to update the user input
        /// or an object message for the file data information.
        /// </summary>
	    public void ReceiveMessages()
	    {
		    string fullPath = "";
		    string receivedStringMsg = "";
		    int readLength = 0;
            int displayId = 0;

		    // Read the first three bytes and check if message is either a STR or OBJ message.
		    byte[] typeBuffer = new byte[3];

            if(networkStream.CanRead)
            {
                networkStream.Read(typeBuffer, 0, typeBuffer.Length);
                // Encode to utf-8 format.
                type = System.Text.Encoding.UTF8.GetString(typeBuffer);
                Debug.Log("The type of the message received from server: " + type);

                if(type.Equals("STR"))
                {
                    displayId = networkStream.ReadByte();
                    readLength = networkStream.ReadByte();
                    byte[] buffer = new byte[readLength];
                    networkStream.Read(buffer, 0, buffer.Length);
                    receivedStringMsg = System.Text.Encoding.UTF8.GetString(buffer);
                    Debug.Log("The user input message received from server: " + receivedStringMsg);

                    lock (inboxMessages)
                    {
                        inboxMessages.Enqueue(new KeyValuePair<int, string>(displayId, receivedStringMsg));
                    }
                }

                if(type.Equals("OBJ"))
                {
                    try
                    {
                        FileDataInformation fileData = Serializer.DeserializeWithLengthPrefix<FileDataInformation>(networkStream, PrefixStyle.Base128);

                        if(fileData != null)
                        {
                            Debug.Log("display ID: " + fileData.DisplayId);
                            Debug.Log("File name: " + fileData.FileName);

                            fullPath = SaveToSystemDirectory(fileData.FileName);

                            try
                            {
                                using(BinaryWriter bWrite = new BinaryWriter(File.Open(fullPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite)))
                                {
                                    bWrite.Write(fileData.FileContents);
                                    bWrite.Flush();
                                }
                            }
                            catch(IOException io)
                            {
                                Debug.LogError("Error in writing binary data to file: " + io);
                            }

                            lock(inboxMessages)
                            {
                                inboxMessages.Enqueue(new KeyValuePair<int, string>(fileData.DisplayId, fullPath));
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Debug.LogError("Error in NetworkStream: " + e);
                        return;
                    }
                }
            }
	    }
	

        /// <summary>
        /// Send the user input to the server which is placed on a display.
        /// </summary>
        /// <param name="number">The display ID, which detects an input.</param>
        /// <param name="message">The user input message.</param>
	    public void SendUserInputToServer(int number, string message)
	    {
            try
            {
                if(networkStream.CanWrite)
                {
                    Debug.Log("Message from GameController: " + "display ID: " + number + ",  message: " + message);
                    networkStream.Write(System.Text.Encoding.UTF8.GetBytes("STR"), 0, "STR".Length);
                    // Send the display ID to server.
                    networkStream.Write(BitConverter.GetBytes(number), 0, 1);
                    // Send the length of the message to server.
                    networkStream.Write(BitConverter.GetBytes(message.Length), 0, 1);
                    // Send the message itself to server.
                    networkStream.Write(System.Text.Encoding.UTF8.GetBytes(message), 0, message.Length);
                    Debug.Log("Message is sent to server: display ID: " + number + ", message: " + message);
                }
                else
                {
                    Debug.LogError("Can't write to NetworkStream.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error during writing to the NetworkStream " + e);
            }
	    }


        /// <summary>
        /// Set the user input to the game controller.
        /// </summary>
        /// <param name="displayId">The display ID received from server.</param>
        /// <param name="message">The message received from server.</param>
        public void SetUserInputToGameController(int displayId, string message)
	    {
            gameControllerObject = GameObject.Find("GameControllerObject");
            if(gameControllerObject != null)
            {
                gameController = gameControllerObject.GetComponent<GameController>();
                Debug.Log("Client sets the user input to GameController: " + " display ID: " + displayId + ", message: " + message);
                gameController.SetUserInputToAllDisplays(displayId, message);
            }

            else
            {
                Debug.LogWarning("Game controller object does not exist.");
            }
	    }


        /// <summary>
        /// Set the received file to game controller.
        /// </summary>
        /// <param name="displayId">The display ID received from server.</param>
        /// <param name="filePath">The file path with the path to system directory and file name.</param>
        public void SetFileToGameController(int displayId, string filePath)
	    {
            gameControllerObject = GameObject.Find("GameControllerObject");
            if(gameControllerObject != null)
            {
                gameController = gameControllerObject.GetComponent<GameController>();
                gameController.SetFileToDisplay(displayId, filePath, tempDirectoryPath);
            }
            else
            {
                Debug.LogWarning("Game controller object does not exist.");
            }
	    }
	

        /// <summary>
        /// Delete the system directory if it is currently available.
        /// Create a new system directory if there is no directory currently available.
        /// </summary>
	    public void ResetSystemDirectory()
	    {
		    try
		    {
			    if(Directory.Exists(tempDirectoryPath))
			    {
				    // Delete the directory with all files.
				    Directory.Delete(tempDirectoryPath, true);
				    Debug.Log("Old directory is deleted.");
				
				    // Create a new directory.
				    Directory.CreateDirectory(tempDirectoryPath);
				    Debug.Log("New directory is created.");
			    }

			    else
			    {
				    Directory.CreateDirectory(tempDirectoryPath);
				    Debug.Log("New directory is created.");
			    }
		    }
		    catch(Exception e)
		    {
			    Debug.LogError("Path " + tempDirectoryPath + " has some problems: " + e);
		    }
	    }
	

        /// <summary>
        /// A new temp folder is created inside the system temp folder.
        /// </summary>
	    public void SetPathToSystemDirectory()
	    {
		    // Get system temp folder.
		    string path = Environment.GetEnvironmentVariable("temp", EnvironmentVariableTarget.Machine);
            // Create a new temp folder inside the OS temp folder.
            tempDirectoryPath = Path.Combine(path, "temp");
	    }
	

	    /// <summary>
        /// Save the received file from server to the client system directory
        /// and check for file name correctness in case it is a PDF file.
        /// </summary>
        /// <param name="fileName">The received file name from server.</param>
        /// <returns>fullPath</returns>
	    public string SaveToSystemDirectory(string fileName)
	    {
            string validFileName = "";
            if(fileName.ToLower().EndsWith(".pdf"))
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                // Check if file name is valid, otherwise ghostscript can't execute them.
                if(!IsValidFileName(fileNameWithoutExtension))
                {
                    // In case of invalid characters, replace them with "-".
                    validFileName = Regex.Replace(fileNameWithoutExtension, "[^a-zA-Z0-9_-]+", "-") + ".pdf";
                }
                else
                {
                    validFileName = fileName;
                }
            }
            else
            {
                validFileName = fileName;
            }
            string tempDirPath = tempDirectoryPath;
		    string fullPath = Path.Combine(tempDirPath, validFileName);
		    Debug.Log("File saved to directory: " + fullPath);
		    return fullPath;
	    }


        /// <summary>
        /// Check if the received file name is a valid file name.
        /// </summary>
        /// <param name="fileName">The received file name from server</param>
        /// <returns>true or false</returns>
        public bool IsValidFileName(string fileName)
        {
            Regex invalidCharacters = new Regex("[^a-zA-Z0-9_-]+");
            if(invalidCharacters.IsMatch(fileName))
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        /// <summary>
        /// Close the connection between client and server.
        /// </summary>
        public void CloseConnection()
	    {
            if(IsClientConnected)
            {
                isConnected = false;
		        clientThread.Abort();
		        networkStream.Close();
                Debug.Log("Client closed.");
            }
		
        }

        /// <summary>
        /// Get and set to check if client is connected.
        /// </summary>
        public bool IsClientConnected
        {
            get
            {
                return isConnected;
            }
            set
            {
                isConnected = value;
            }
        }


        // Unity function: Close client connection, if application is closed.
	    void OnApplicationQuit()
	    {
		    CloseConnection();
	    }
    }
}
