using UnityEngine;
using System;


namespace Ghostscript
{
    /// <summary>
    /// The Ghostscript wrapper to call the API with the specified arguments.
    /// </summary>
    public class GhostscriptWrapper
    { 
        /// <summary>
        /// Convert a PDF-file to one or multiple PNG-files.
        /// </summary>
        /// <param name="inputFilePath">The input PDF-File.</param>
        /// <param name="outputFilePath">The output PNG-file(s).</param>
        public static void ConvertPdfToPng(string inputFilePath, string outputFilePath)
        {
            try
            {
                // Check if ghostscript is needed as 32-bit or 64-bit.
                if(IntPtr.Size == 4)
                {
                    GhostscriptSharp.API.GhostScript32.CallAPI(GetArgs(inputFilePath, outputFilePath));
                }
                else
                {
                    GhostscriptSharp.API.GhostScript64.CallAPI(GetArgs(inputFilePath, outputFilePath));
                }
            }
            catch (Exception e)
            {
                Debug.Log("Error in converting PDF to PNG with ghostscript: " + e);
            }

        }

        /// <summary>
        /// The arguments needed to convert multiple pages from a PDF-file.
        /// </summary>
        /// <param name="inputFilePath">The input file path (PDF-file).</param>
        /// <param name="outputFilePath">The output file path (PNG-file(s)).</param>
        /// <returns></returns>
        private static string[] GetArgs(string inputFilePath, string outputFilePath)
        {
            string[] commands = new string[5];
            Debug.Log("input file path: " + inputFilePath + ", \n output file path: " + outputFilePath);
            // commands[0] cannot be used, because it is ignored by the Ghostscript API.
            commands[1] = "-sDEVICE=png16m"; // 24 Bit RGB
            commands[2] = "-o"; 
            commands[3] = outputFilePath + "-%d" + ".png";
            commands[4] = inputFilePath;

            return commands;
        }
    }
}