using BLPT.Brutal;

using System;
using System.IO;

namespace BLPT
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(@"______ _    ______ _____ ");
            Console.WriteLine(@"| ___ \ |   | ___ \_   _|");
            Console.WriteLine(@"| |_/ / |   | |_/ / | |  ");
            Console.WriteLine(@"| ___ \ |   |  __/  | |  ");
            Console.WriteLine(@"| |_/ / |___| |     | |  ");
            Console.WriteLine(@"\____/\_____|_|     \_/  ");
            Console.WriteLine(string.Empty);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Brutal Legend Package Tamper");
            Console.WriteLine("0xC0DED by gdkchan");
            Console.WriteLine("Version 0.1.3");
            Console.ResetColor();
            Console.WriteLine(string.Empty);

            if (args.Length != 4)
                PrintError("Invalid number of arguments!");
            else
            {
                if (!File.Exists(args[1]))
                    PrintError(string.Format("File \"{0}\" not found!", args[1]));
                else if (!File.Exists(args[2]))
                    PrintError(string.Format("File \"{0}\" not found!", args[2]));
                else
                {
                    BrutalPackage.OnStatusReport += StatusReportCallback;

                    switch (args[0])
                    {
                        case "-e":
                            if (BrutalPackage.Extract(args[1], args[2], args[3]))
                                PrintSuccess("The operation was completed successfully!");
                            else
                                PrintError("Invalid or corrupted header file specified!");
                            break;

                        case "-i":
                            if (BrutalPackage.Insert(args[1], args[2], args[3]))
                                PrintSuccess("The operation was completed successfully!");
                            else
                                PrintError("Invalid or corrupted header file specified!");
                            break;

                        default: PrintError("Invalid option specified, use \"-e\" or \"-i\"!"); break;
                    }
                }
            }
        }

        static void PrintSuccess(string Message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Message);
            Console.ResetColor();
        }

        static void PrintError(string Message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ERROR: " + Message);
            Console.ResetColor();
            PrintUsage();
        }

        static void PrintUsage()
        {
            Console.WriteLine(string.Empty);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Usage:");
            Console.ResetColor();

            Console.WriteLine(string.Empty);
            Console.WriteLine("To extract files from a package:");
            Console.WriteLine("BLPT -e file.~h file.~p out_folder");

            Console.WriteLine(string.Empty);
            Console.WriteLine("To insert files into a package:");
            Console.WriteLine("BLPT -i file.~h file.~p in_folder");

            Console.WriteLine(string.Empty);
            Console.WriteLine("To work with Xbox 360 packages, you need the following files:");
            Console.WriteLine("- xbcompress.exe");
            Console.WriteLine("- xbdecompress.exe");
            Console.WriteLine("- xbdm.dll");
            Console.WriteLine("- msvcp71.dll (VC++ dependency)");
            Console.WriteLine("- msvcr71.dll (VC++ dependency)");
            Console.WriteLine("Those files can be found on the Xbox 360 SDK.");
            Console.WriteLine("For PS3 packages, you don't need any additional files.");
        }

        static void StatusReportCallback(object Sender, BrutalStatusReport Report)
        {
            int Percentage = (int)(((float)Report.ProcessedFiles / Report.TotalFiles) * 100);
            string Percent = string.Format("[{0,3}%] ", Percentage);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(Percent);
            Console.ResetColor();
            Console.WriteLine(Report.Status);
        }
    }
}
