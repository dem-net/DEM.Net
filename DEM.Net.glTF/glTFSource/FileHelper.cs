using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace AssetGenerator
{
    public static class FileHelper
    {
        /// <summary>
        ///  Moves and then deletes output from previous runs, to help avoid retaining stale output.
        /// </summary>
        public static void ClearOldFiles(string executingAssemblyFolder, string assetFolder)
        {
            var trashFolder = Path.Combine(executingAssemblyFolder, "Delete");
            bool tryAgain = true;
            while (tryAgain)
            {
                try
                {
                    Directory.Move(assetFolder, trashFolder);
                    Directory.Delete(trashFolder, true);
                    tryAgain = false;
                }
                catch (DirectoryNotFoundException)
                {
                    // Do nothing
                    tryAgain = false;
                }
                catch (IOException)
                {
                    Console.WriteLine("Unable to delete the directory.");
                    Console.WriteLine("Verify that there are no open files and that the current user has write permission to that directory.");
                    Console.WriteLine("Press any key to try again.");
                    Console.ReadKey();
                    tryAgain = true;
                }
            }
        }

        /// <summary>
        ///  Builds a list of the names of each file in the targeted folder, to be later useded in creating image URIs.
        ///  Only looks at files in folders in the directory, and only one level deep.
        /// </summary>
        public static List<string> FindImageFiles(string imageFolder)
        {
            List<string> images = new List<string>();
            foreach (string folder in Directory.GetDirectories(imageFolder))
            {
                foreach (string image in Directory.EnumerateFiles(folder))
                {
                    images.Add(FormatForUri(Path.Combine(Path.GetFileName(folder), Path.GetFileName(image))));
                }
            }

            return images;
        }

        /// <summary>
        /// Copies images from the resources folder into the Output directory
        /// </summary>
        public static void CopyImageFiles(string executingAssemblyFolder, string outputFolder, List<Runtime.Image> usedImages, bool useThumbnails = false)
        {
            if (usedImages.Count > 0)
            {
                foreach (var image in usedImages)
                {
                    string name = FormatForFileSystem(image.Uri.ToString());

                    var source = Path.Combine(executingAssemblyFolder, "Resources", name);
                    var destination = Path.Combine(outputFolder, name);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.Copy(source, destination, true);
                }
            }

            if (useThumbnails == true)
            {
                CopyThumbnailImageFiles(executingAssemblyFolder, outputFolder, usedImages);
            }
        }

        /// <summary>
        /// Starts the copy for the thumbnail for a given list of images
        /// </summary>
        static void CopyThumbnailImageFiles(string executingAssemblyFolder, string outputFolder, List<Runtime.Image> usedImages)
        {
            // Use the list of images to infer the list of thumbnails
            List<Runtime.Image> usedThumbnailImages = new List<Runtime.Image>();

            // Change the file path to one used by the thumbnails
            foreach (var image in usedImages)
            {
                usedThumbnailImages.Add(
                    new Runtime.Image()
                    {
                        Uri = FormatForUri(Path.Combine("Figures", "Thumbnails", Path.GetFileName(image.Uri.ToString())))
                    });
            }

            // Copy those thumbnails to the destination directory
            CopyImageFiles(executingAssemblyFolder, outputFolder, usedThumbnailImages);
        }

        /// <summary>
        /// Converts the seperators in a relative local path into those needed for a Uri.
        /// For use in building a Uri for an image.
        /// </summary>
        static string FormatForUri(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, '/');
        }

        /// <summary>
        /// Converts the seperators in a uri string into a relative local path.
        /// For use in recreating the local path from a Uri.
        /// </summary>
        static string FormatForFileSystem(string path)
        {
            return path.Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
