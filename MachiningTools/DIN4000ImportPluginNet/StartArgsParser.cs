using System.IO.Compression;
namespace DIN4000ImportPlugin
{

    public class StartArgs
    {
        public List<string> ZipFiles;
        public List<string> CsvFiles;
        public string ResultDBFile;
        public string SCInstallFolder = @"C:\Program Files\ENCY Software\ENCY";

        public StartArgs()
        {
            ZipFiles = new List<string>();
            CsvFiles = new List<string>();
            ResultDBFile = "";
        }

        public void Parse(string[] args)
        {
            if (args==null)
                return;
            for (int i = 0; i < args.Length; i++)
            {
                string p = args[i];
                string ext = Path.GetExtension(p);
                if (String.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase))
                    CsvFiles.Add(p);
                else if (String.Equals(ext, ".zip", StringComparison.OrdinalIgnoreCase))
                    ZipFiles.Add(p);
                else if (String.Equals(ext, ".db", StringComparison.OrdinalIgnoreCase))
                    ResultDBFile = p;
                else if (p.StartsWith("SCInstallFolder:"))
                {
                    SCInstallFolder = p.Substring(16);
                    if (SCInstallFolder.StartsWith('"'))
                        SCInstallFolder = SCInstallFolder.Substring(1, SCInstallFolder.Length - 2);
                }
            }
        }

        private string TmpUnzipDir { get => Path.GetTempPath() + @"DIN4000ImportPlugin"; }

        public void UnZipCsvFiles()
        {
            foreach (string zipFileName in ZipFiles)
            {
                var tmpPath = TmpUnzipDir;
                var zip = ZipFile.OpenRead(zipFileName);
                foreach (ZipArchiveEntry e in zip.Entries)
                {
                    if (e.FullName.EndsWith("_DIN4000.csv", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!Directory.Exists(tmpPath))
                            Directory.CreateDirectory(tmpPath);
                        string csvFileName = tmpPath  + @"\" + Path.GetFileNameWithoutExtension(zipFileName) + ".csv";
                        e.ExtractToFile(csvFileName, true);
                        CsvFiles.Add(csvFileName);
                        break;
                    }
                }
            }
        }

        public void ClearUnzippedCsvFiles()
        {
            var tmpPath = TmpUnzipDir;
            if (Directory.Exists(tmpPath))
                Directory.Delete(tmpPath, true);
        }
    }
}