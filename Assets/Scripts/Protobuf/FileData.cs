using ProtoBuf;

/// <summary>
/// The Protobuf-message to serialize and deserialize the file object.
/// </summary>
namespace FileData
{
    /// <summary>
    /// This class contains the information for a file.
    /// </summary>
    [ProtoContract]
    public class FileDataInformation
    {
        /// <summary>
        /// The display ID.
        /// </summary>
        [ProtoMember(1)]
        private int displayId;

        /// <summary>
        /// The file name which is send from server to client.
        /// </summary>
        [ProtoMember(2)]
        private string fileName;

        /// <summary>
        /// The file content as bytes to write the file.
        /// </summary>
        [ProtoMember(3)]
        private byte[] fileContents;

        /// <summary>
        /// Get and set the display ID.
        /// </summary>
        public int DisplayId
        {
            get { return displayId; }
            set { displayId = value; }
        }

        /// <summary>
        /// Get and set the file name.
        /// </summary>
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        /// <summary>
        /// Get and set the file content.
        /// </summary>
        public byte[] FileContents
        {
            get { return fileContents; }
            set { fileContents = value; }
        }
    }
}
