using Ghostscript;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Threading;
using Domain;


namespace DisplayApplication
{
    /// <summary>
    /// This class represents an instance of a display. It has an unique number
    /// called display ID. Therefore, it is possible to assign a file to the 
    /// corresponding display.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(AudioSource))]
    public class DisplayController : MonoBehaviour
    { 
        /// <summary>
        /// The display ID from this game object.
        /// </summary>
        public int displayId;

        /// <summary>
        /// This list is responsible to store all images converted from 
        /// a PDF file.
        /// </summary>
        private List<string> filePathList;

        /// <summary>
        /// The total number of pages from a PDF file.
        /// </summary>
	    private int totalNumberOfPages;

        /// <summary>
        /// Check if a new file is sent to the display.
        /// </summary>
        private bool refresh;

        /// <summary>
        /// Check if the received file is an image file.
        /// </summary>
        private bool isImageFile;

        /// <summary>
        /// Check if the received file is a video file.
        /// </summary>
	    private bool isVideoFile;

        /// <summary>
        /// Check if the received file is a PDF file.
        /// </summary>
	    private bool isPdfFile;

        /// <summary>
        /// Check if display is selected.
        /// </summary>
	    private bool displaySelected;

        /// <summary>
        /// The access to web pages from Unity.
        /// This variable contains the file which should be loaded.
        /// </summary>
	    private WWW loadFile;

        /// <summary>
        /// The prefix to load a local file from disk.
        /// </summary>
        private readonly string prefix = @"file:///";

        /// <summary>
        /// Get information by a hit.
        /// </summary>
	    private RaycastHit hit;

        /// <summary>
        /// Get the position.
        /// </summary>
	    private Ray ray;

        /// <summary>
        /// The movie texture added to the display.
        /// </summary>
	    private MovieTexture movieTexture;

        /// <summary>
        /// The object, the game controller script is attached to.
        /// </summary>
        private GameObject gameControllerObject;

        /// <summary>
        /// The game controller script component.
        /// </summary>
	    private GameController gameController;

        /// <summary>
        /// The single page of an image from a converted PDF file.
        /// </summary>
	    private int page;

        /// <summary>
        /// The file thread to start a parallel process for convertion.
        /// </summary>
        private Thread fileThread;

        /// <summary>
        /// Create a new empty texture to assign it from www to display.
        /// </summary>
        private Texture2D displayTexture;

        /// <summary>
        /// Counter variable to number the image files stored in the filePathList.
        /// </summary>
        private int pageCount;

        /// <summary>
        /// The PDF-Thread to load a converted image from PDF file.
        /// </summary>
        private Thread pdfThread;

        /// <summary>
        /// It shows if ghostscript has finished to convert a PDF file to multiple images.
        /// </summary>
        private bool isFinished;


        // Unity function: Use this for initialization.
        void Start()
        {
            refresh = false;
            isImageFile = false;
            isVideoFile = false;
            isPdfFile = false;
		    displaySelected = false;
            isFinished = false;
            totalNumberOfPages = 0;
            filePathList = new List<string>();
            displayTexture = new Texture2D(4, 4);
            pageCount = 0;
            page = 0;

            gameControllerObject = GameObject.Find("GameControllerObject");
            if(gameControllerObject != null)
            {
                gameController = gameControllerObject.GetComponent<GameController>();
            }
            else
            {
                Debug.LogWarning("Game controller object does not exist.");
            }
	    }


        // Unity function: Update is called once per frame.
        void Update()
        {
            if(refresh)
            {
                /* 
                 * Check if file is an image file, video file or PDF file and assign the corresponding
                 * texture to the display.
                 */
                if(isImageFile)
                {
                    gameObject.GetComponent<MeshRenderer>().enabled = true;
                
                    loadFile.LoadImageIntoTexture(displayTexture);
                    gameObject.GetComponent<Renderer>().material.mainTexture = displayTexture;

                    isImageFile = false;
                    refresh = false;
                }

                if(isVideoFile)
                {
                    gameObject.GetComponent<MeshRenderer>().enabled = true;
                    movieTexture = loadFile.movie;
                    gameObject.GetComponent<Renderer>().material.mainTexture = movieTexture;
                    gameObject.GetComponent<AudioSource>().clip = movieTexture.audioClip;

                    movieTexture.Play();
                    gameObject.GetComponent<AudioSource>().Play();

				    isVideoFile = false;
                    refresh = false;
                }

                if(isPdfFile)
                {
                    ChangePageOfPdfFile();   
                }
            }

            // Allow to click left and right arrow, only if display is selected by mouse click.
		    if(displaySelected)
		    {
                SendUserButtonInputForFile();
		    }
		    CheckMouseClick();
        }


        /// <summary>
        /// Refreshes the file on display.
        /// </summary>
        /// <param name="filePath">The file path which is sent by game controller.</param>
        /// <param name="tempDirectoryPath">The directory to the system temp folder.</param>
        public void Refresh(string filePath, string tempDirectoryPath)
        {
            Reset();
            CheckFileExtension(filePath, tempDirectoryPath);

            if(isImageFile || isVideoFile)
            {
                Debug.Log("Refresh is activated: Set the file to the corresponding display.");
                refresh = true;
            }
        }


        /// <summary>
        /// Resets the variables if a new file is sent to the display.
        /// </summary>
        public void Reset()
        {
            gameObject.GetComponent<Renderer>().material.mainTexture = null;
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            filePathList.Clear();
            loadFile = null;
            movieTexture = null;
            isFinished = false;
            pageCount = 0;
            page = 0;
        }


        /// <summary>
        /// Checks if the file extension is an image, video or PDF file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="tempDirectoryPath">The path to the temp directory to create a new folder inside it.</param>
        public void CheckFileExtension(string filePath, string tempDirectoryPath)
        {
            if(filePath.ToLower().EndsWith(".jpg") || filePath.ToLower().EndsWith(".jpeg") 
                || filePath.ToLower().EndsWith(".png"))
            {
                LoadFile(filePath);
                Debug.Log("Image file is selected");
                isImageFile = true;
            }
            else if(filePath.ToLower().EndsWith(".ogg") || filePath.ToLower().EndsWith(".ogv"))
            {
                LoadFile(filePath);
			    gameObject.AddComponent<AudioSource>();
                Debug.Log("Video file is selected");
                isVideoFile = true;
            }
            else if(filePath.ToLower().EndsWith(".pdf"))
            {
                CheckPdfDirectory(filePath, tempDirectoryPath);
            }
            else
            {
                Debug.LogWarning("The file format is not supported. \n Please choose JPG or PNG "
                                + "for image format, OGG and OGV for video format or a PDF file.");
            }
        }


        /// <summary>
        /// Checks the PDF directory. Stores the image data in a list, 
        /// if there is already an image folder for the PDF file.
        /// Otherwise, start a thread for the convertion from PDF to PNG.
        /// </summary>
        /// <param name="filePath">The file path to system directory including file name.</param>
        /// <param name="tempDirectoryPath">The path to temp folder without file name.</param>
        public void CheckPdfDirectory(string filePath, string tempDirectoryPath)
        {

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string pdfImageFilePath = CreatePathForPdfImages(tempDirectoryPath, fileName);

            /*
            * Gets the parent directory that contains all the PDF image files
            * and count how many files are inside the folder. 
            * The number of images represents the number of pages in the PDF file.
            */
            string topDirectory = Directory.GetParent(pdfImageFilePath).FullName;
            totalNumberOfPages = GetTotalNumberOfPdfPages(topDirectory);

            // If image pages already exist in directory, take the images to display.
            if (totalNumberOfPages > 0)
            {
                Debug.Log("Images for " + "'" + fileName + "'" + " already exist.");
                for (int p = 1; p <= totalNumberOfPages; p++)
                {
                    // Each image of PDF file gets a page number to identify it clearly.
                    filePathList.Add(pdfImageFilePath + "-" + p + ".png");
                    Debug.Log(p + " " + fileName);
                }
                Debug.Log("The file path of PDF file: " + pdfImageFilePath + ", Length: " + totalNumberOfPages);
                isPdfFile = true;
                refresh = true;
            }
            // If the image pages do not exist in directory, execute ghostscript to get them.
            else
            {
                // Lambda expression to start a parameterized thread.
                pdfThread = new Thread(() => LoadPdfImages(topDirectory, pdfImageFilePath));
                pdfThread.Start();

                fileThread = new Thread(() => StoreImageDataFromPdf(filePath, pdfImageFilePath, fileName));
                fileThread.Start();
            }

            // Lambda expression to start a parameterized thread.
            pdfThread = new Thread(() => LoadPdfImages(topDirectory, pdfImageFilePath));
            pdfThread.Start();

            fileThread = new Thread(() => StoreImageDataFromPdf(filePath, pdfImageFilePath, fileName));
            fileThread.Start();
        }


        /// <summary>
        /// A parallel process to search for images which are already finished from convertion by ghostscript.
        /// </summary>
        /// <param name="topDirectory">The parent directory to count the total number of pages.</param>
        /// <param name="pdfImageFilePath">The path to the images converted from a pdf file.</param>
        public void LoadPdfImages(string topDirectory, string pdfImageFilePath)
        {
            Debug.Log("Start PDF-Thread.");
            while(!isFinished)
            {
                totalNumberOfPages = GetTotalNumberOfPdfPages(topDirectory);
                Debug.Log(totalNumberOfPages);
                if(totalNumberOfPages >= 1 && pageCount <= totalNumberOfPages)
                {
                    pageCount++;
                    filePathList.Add(pdfImageFilePath + "-" + pageCount + ".png");

                    if(totalNumberOfPages == 1)
                    {
                        isPdfFile = true;
                        refresh = true;
                    }
                }
            }
            Debug.Log("Stop PDF-Thread.");
            pdfThread.Abort();
        }


        /// <summary>
        /// Stores the image data from PDF file. Each page of a PDF file will be converted
	    /// to a separate image.
        /// </summary>
        /// <param name="filePath">The file path to system directory.</param>
        /// <param name="pdfImageFilePath">The image file path. The images are the ones which are converted through a PDF file.</param>
        /// <param name="fileName">The file name of the PDF file.</param>
        public void StoreImageDataFromPdf(string filePath, string pdfImageFilePath, string fileName)
        {
            Debug.Log("Ghostscript is now converting the PDF for file name: " + fileName);
            GhostscriptWrapper.ConvertPdfToPng(filePath, pdfImageFilePath);
            isFinished = true;

            // Finish the thread if it is not used anymore.
            fileThread.Abort();
        }


        /// <summary>
        /// Saves the PDF images to system directory.
        /// </summary>
        /// <returns>The path to the pdf images directory.</returns>
        /// <param name="tempDirectoryPath">The system directory temp path.</param>
        /// <param name="fileName">The file name of the PDF file.</param>
        public string CreatePathForPdfImages(string tempDirectoryPath, string fileName)
        {
            string pdfImageTempFolder = Path.Combine(tempDirectoryPath, "pdfImageTemp"+displayId);

            if(!Directory.Exists(pdfImageTempFolder))
            {
                Directory.CreateDirectory(pdfImageTempFolder);
            }
            else
            {
                Debug.Log("Directory " + pdfImageTempFolder + " already exists.");
            }

            /*
		     * Assigns an individual folder for every pdf. The corresponding images are
		     * located in this folder.
		     */
            string pdfImageTempFileName = Path.Combine(pdfImageTempFolder, fileName);

            if(!Directory.Exists(pdfImageTempFileName))
            {
                Directory.CreateDirectory(pdfImageTempFileName);
            }
            else
            {
                Debug.Log("Directory " + pdfImageTempFileName + " already exists.");
            }

            string fullPath = Path.Combine(pdfImageTempFileName, fileName);
            Debug.Log("This is the PDF path: " + fullPath);
            return fullPath;
        }

        /// <summary>
        /// Gets the total number of PDF pages by searching for the number of image files in directory.
        /// </summary>
        /// <returns>The total number of PDF pages converted to images.</returns>
        /// <param name="path">The file path.</param>
        public int GetTotalNumberOfPdfPages(string path)
        {
            if(Directory.Exists(path))
            {
                totalNumberOfPages = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly).Length;
            }
            else
            {
                Debug.Log("The directory " + path + " does not exist.");
                pdfThread.Abort();
            }
            return totalNumberOfPages;
        }

        /// <summary>
        /// Change the page of the PDF file. The next or previous image will be shown.
        /// </summary>
        public void ChangePageOfPdfFile()
        {
            LoadFile(filePathList[page]);
            // Only texture which have not the width and height of 8 can be loaded.
            if (loadFile.texture.width != 8 && loadFile.texture.height != 8)
            {
                loadFile.LoadImageIntoTexture(displayTexture);
                if (displayTexture != null)
                {
                    gameObject.GetComponent<MeshRenderer>().enabled = true;
                    gameObject.GetComponent<Renderer>().material.mainTexture = displayTexture;
                }
                else
                {
                    Debug.LogError("Couldn't open file: " + filePathList[page]);
                }

                isPdfFile = false;
                refresh = false;
            }
        }

	    /// <summary>
	    /// Load the file from disk.
	    /// </summary>
	    /// <param name="filePath">The file path to load.</param>
        public void LoadFile(string filePath)
        {
            loadFile = new WWW(prefix + filePath);
            while(!loadFile.isDone)
            {
                continue;
            }
            Debug.Log("The file: " + filePath + " is successfully loaded.");
        }


        // User input functions.

	    /// <summary>
	    /// Checks the mouse click from a user. If the mouse click hits a display,
	    /// it will be identified which display is met.
	    /// </summary>
        public void CheckMouseClick()
        {
            // This is the button code for the left mouse button.
            if(Input.GetMouseButtonDown(0))
            {
                displaySelected = false;
                ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if(Physics.Raycast(ray, out hit))
                {
                    if(hit.transform.name.Equals(gameObject.name))
                    {
                        Debug.Log("Display with ID " + displayId + " was clicked.");
                        /* 
                         * Check which user input is made.
					     * It can be either for PDF or video file.
					     */
                        displaySelected = true;
                        SendUserClickInput();
                    }
                }
            }
        }


	    /// <summary>
        /// Check if an user input is made on PDF file or video file.
        /// The PDF file contains the left and right arrow for previous and next slide.
        /// The video file contains the space key to stop the video completely or 
        /// repeat it.
        /// </summary>
	    public void SendUserButtonInputForFile()
	    {
		    string message = "";
            // Make user input possible only if it is not a video file or image file.
            if(movieTexture == null && filePathList.Count != 0)
            {
                if(Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    if(page > 0)
                    {
                        message = "previousSlide";
                    }
                }

                if(Input.GetKeyDown(KeyCode.RightArrow))
                {
                    if(page < (totalNumberOfPages - 1))
                    {
                        message = "nextSlide";
                    }
                }
            }
            else
            {
                if(Input.GetKeyDown(KeyCode.Space))
                {
                    // Stop movie and begin again.
                    message = "stopVideo";
                }
            }

		    if(!message.Equals(""))
		    {
			    gameController.SetUserInputToClient(displayId, message);
                Debug.Log("Set message to client: " + message);
            }
	    }
 

        /// <summary>
        /// Checks user input for a file which was made on a specific display. 
        /// It can be either a video file to pause or play the video or a PDF file
        /// to go to next or previous page.
        /// The user input is send to the game controller that informs the client.
        /// </summary>
        public void SendUserClickInput()
	    {
            string message = "";

            /* Check if display has a movie texture.
             * It yes, then check the current state.
             * The movie texture can be played, paused or completely stopped.
             */
            if(movieTexture != null)
            {
                if(movieTexture.isPlaying)
                {
                    message = "pauseVideo";
                }

                else
                {
                    message = "playVideo";
                }
            }

            if(!message.Equals(""))
            {
                gameController.SetUserInputToClient(displayId, message);
                Debug.Log("Set message to client: " + "display ID: " + displayId + " Message: " + message);
            }
	    }


        // These functions are called from game controller to synchronize the user input of the display for all clients.

        /// <summary>
        /// Sets the user input of a video file for play or pause.
        /// </summary> 
        /// <param name="number">The received display ID.</param>
        public void SetUserInputForPlayAndPause(int number)
        {
            if(displayId == number)
            {
                if(movieTexture != null)
                {
                    if(movieTexture.isPlaying)
                    {
                        movieTexture.Pause();
                    }

                    else
                    {
                        movieTexture.Play();
                    }
                }
            }
        }


        /// <summary>
        /// Stop video and play it again automatically.
        /// </summary>
        /// <param name="number">The received display ID.</param>
        public void SetUserInputForStop(int number)
        {
            if(displayId == number)
            {
                if(movieTexture != null)
                {
                    movieTexture.Stop();
                    movieTexture.Play();
                    gameObject.GetComponent<AudioSource>().Play();
                }
            }
        }

        /// <summary>
        /// Sets the previous page for PDF file.
        /// </summary>
        /// <param name="number">The received display ID.</param>
        public void SetPreviousPageForPdfFile(int number)
        {
            if(displayId == number)
            {
                if(page > 0)
                {
                    page -= 1;
                    isPdfFile = true;
                    refresh = true;
                }
            }
        }

        /// <summary>
        /// Sets the next page for PDF file.
        /// </summary>
        /// <param name="number">The received display ID.</param>
        public void SetNextPageForPdfFile(int number)
        {
            if(displayId == number)
            {
                if(page < (totalNumberOfPages - 1))
                {
                    page += 1;
                    isPdfFile = true;
                    refresh = true;
                }
            }
        }


        /// <summary>
        /// Abort the threads if the application is closed.
        /// </summary>
        void OnApplicationQuit()
        {
            isFinished = true;
            if(fileThread != null)
            {
                fileThread.Abort();
            }

            if(pdfThread != null)
            {
                pdfThread.Abort();
            }
        }
    }
}
