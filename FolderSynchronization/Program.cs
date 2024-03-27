using System.Security.Cryptography;

internal class Program
{
    //Log file path (this has a default value for the IDE only, it's not used)
    private static string logFilePath = "example.txt";

    private static void Main(string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Usage: program.exe <source_folder> <replica_folder> <sync_interval_seconds> <log_file_path>");
            return;
        }

        string sourceFolder = args[0];
        string replicaFolder = args[1];

        if (!Directory.Exists(sourceFolder))
        {
            Console.WriteLine("Source folder does not exist.\nProgram shutting down.");
            return;
        }

        int syncIntervalSeconds;
        if (!int.TryParse(args[2], out syncIntervalSeconds) || syncIntervalSeconds <= 0)
        {
            Console.WriteLine("Invalid sync interval. Please provide a positive integer value for sync_interval_seconds.");
            return;
        }

        logFilePath = args[3];

        Console.WriteLine($"Source Folder: {sourceFolder}");
        Console.WriteLine($"Replica Folder: {replicaFolder}");
        Console.WriteLine($"Sync Interval: {syncIntervalSeconds} seconds");
        Console.WriteLine($"Log File Path: {logFilePath}");

        //This loops in an interval marked by the Thread.Sleep
        while (true)
        {
            LogToFile("Starting synchronization.");
            SynchronizeFolders(sourceFolder, replicaFolder);
            LogToFile("Synchronization complete.");
            Thread.Sleep(syncIntervalSeconds * 1000);
        }
    }

    //This is a recurse function that is recalled for every folder
    private static void SynchronizeFolders(string sourceFolder, string replicaFolder)
    {
        if (!Directory.Exists(sourceFolder))
        {
            LogToFile("Source folder stopped existing!!!");
            return;
        }

        if (!Directory.Exists(replicaFolder))
        {
            LogToFile($"Replica folder '{replicaFolder}' created.");
            Directory.CreateDirectory(replicaFolder);
        }

        //For each file in the currect directory
        foreach (string file in Directory.GetFiles(sourceFolder))
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(replicaFolder, fileName);

            //Checks if the file is the same, if it's not the same and if the replica file doesn't exist, copies it to the replica folder
            if ((MD5ToString(file) != MD5ToString(destFile)) && destFile != string.Empty)
            {
                File.Copy(file, destFile, true);
                LogToFile($"Copied '{file}' to '{destFile}'.");
            }
        }

        //For each directory goes recursively to the same function to check every file in the folder
        foreach (string directory in Directory.GetDirectories(sourceFolder))
        {
            string folderName = Path.GetFileName(directory);
            string destFolder = Path.Combine(replicaFolder, folderName);
            SynchronizeFolders(directory, destFolder);
        }

        // This marks the part of deletion of the replica files that don't exist anymore in the source folder

        //For each file in the replica folder deletes if it doesn't exist anymore
        foreach (string file in Directory.GetFiles(replicaFolder))
        {
            string fileName = Path.GetFileName(file);
            string sourceFile = Path.Combine(sourceFolder, fileName);
            if (!File.Exists(sourceFile))
            {
                File.Delete(file);
                LogToFile($"Deleted replica '{file}' file.");
            }
        }

        //For each directory deletes it if it doesn't exist anymore, if it does exist, enter recursively to check further folders
        foreach (string directory in Directory.GetDirectories(replicaFolder))
        {
            string folderName = Path.GetFileName(directory);
            string sourceDir = Path.Combine(sourceFolder, folderName);
            if (!Directory.Exists(sourceDir))
            {
                Directory.Delete(directory, true);
                LogToFile($"Deleted replica '{directory}' directory.");
            }
            else
            {
                SynchronizeFolders(sourceDir, directory);
            }
        }
    }

    // Logs file a message into a file
    private static void LogToFile(string message)
    {
        string logMessage = $"{DateTime.Now} - {message}";

        Console.WriteLine(logMessage);

        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            writer.WriteLine(logMessage);
        }
    }

    //Makes the MD5 of a file and converts it into a string
    private static string MD5ToString(string filePath)
    {
        //If file exists
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}