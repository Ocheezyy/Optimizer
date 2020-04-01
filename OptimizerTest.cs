// Generated code. Do not modify!
//---------------------------------------------------------------------------------------
// Copyright (c) 2001-2019 by PDFTron Systems Inc. All Rights Reserved.
// Consult legal.txt regarding legal and license information.
//---------------------------------------------------------------------------------------

// This version has a FileSystemWatcher that currently doesn't work

using pdftron;
using pdftron.Common;
using pdftron.PDF;
using pdftron.SDF;
using System;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Security.Permissions;
using System.Threading;
using Convert = System.Convert;

namespace PDFNetSamples
{

    class Class1
    {

        private static pdftron.PDFNetLoader pdfNetLoader = pdftron.PDFNetLoader.Instance();

        public class Methods
        {
            [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]

            public static double GetSize(string path)
            {
                var fa = new FileInfo(path);
                double fileSize = fa.Length;
                return fileSize;
            }

            public static double GetDiff(double before, double after)
            {
                double beforePercent = ((after - before) / before);
                double afterPercent = Math.Round(beforePercent, 3, MidpointRounding.AwayFromZero);
                return afterPercent;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "<Pending>")]
            public static void Optimize(string filePath, string fileName, int fileNum)
            {
                PDFNet.Initialize();
                try
                {
                    using (var doc = new PDFDoc(filePath))
                    {
                        // Simple Optimization
                        double beforeSizeOpt1 = GetSize(filePath);
                        doc.InitSecurityHandler();
                        var imageSettings = new Optimizer.ImageSettings();
                            imageSettings.SetCompressionMode(Optimizer.ImageSettings.CompressionMode.e_jpeg);
                            imageSettings.SetQuality(1);
                            imageSettings.SetImageDPI(225, 150);
                            imageSettings.ForceRecompression(true);
                        var optSettings = new Optimizer.OptimizerSettings();
                            optSettings.SetColorImageSettings(imageSettings);
                            optSettings.SetGrayscaleImageSettings(imageSettings);
                        var monoImageSettings = new Optimizer.MonoImageSettings();
                            monoImageSettings.SetImageDPI(450, 300);
                            monoImageSettings.SetCompressionMode(Optimizer.MonoImageSettings.CompressionMode.e_jbig2);
                            monoImageSettings.ForceRecompression(true);
                                optSettings.SetMonoImageSettings(monoImageSettings);
                                
                        Optimizer.Optimize(doc, optSettings);
                        doc.Save(filePath, SDFDoc.SaveOptions.e_linearized);

                        var afterSizeOpt1 = GetSize(filePath);

                        

                        var opt1SizeChange = GetDiff(beforeSizeOpt1, afterSizeOpt1);
                        Console.WriteLine("\n" + opt1SizeChange.ToString("P1",
                                              new NumberFormatInfo
                                              { PercentPositivePattern = 1, PercentNegativePattern = 1 }) +
                                          $" Reduction to: {fileName}");

                        var finalSize = Convert.ToInt32(afterSizeOpt1);

                        // Used for older version where files were saved in a new directory
                        //Directory.CreateDirectory(outputPath + filePath.Substring(41, 7));
                        //doc.Save(outputPath + fileNum + fileName, SDFDoc.SaveOptions.e_linearized);
                        using (var con = new SqlConnection(OptimizerTestCS.Properties.Settings.Default.dbconn)
                        )
                        {
                            con.Open();
                            SqlCommand cmd =
                                new SqlCommand(
                                    $"INSERT INTO optimizerProcessDetailSuccessful (FinalSizeKB, AVSRecNo, FileName, [Path], OptimizedDateTime, Change) VALUES ('{finalSize/1000}', '{fileNum}', '{fileName}', '{filePath.Substring(0, 10)}', '{DateTime.Now}', '{opt1SizeChange}');",
                                    con);
                            cmd.ExecuteNonQuery();
                            con.Close();
                        }
                    }
                }
                catch (PDFNetException e)
                {
                    Console.WriteLine(e.Message);

                    try
                    {
                        SqlCommand cmd;
                        // UNUSED SqlDataAdapter da;
                        using (var con = new SqlConnection(OptimizerTestCS.Properties.Settings.Default.dbconn)
                        )
                        {
                            con.Open();
                            cmd = new SqlCommand(
                                $"INSERT INTO optimizerProcessDetail (AVSRecNo, FileName, ErrorText, ErrorDateTime) VALUES ('{fileNum}', '{fileName}', '{e.Message.Substring(14)}', '{DateTime.Now}');",
                                con);
                            cmd.ExecuteNonQuery();
                            con.Close();
                        }
                    }
                    catch (SqlException sqlException)
                    {
                        Console.WriteLine(sqlException);
                        throw;
                    }
                }
            }
        }

        public static void Run()
        {
            // Create a new FileSystemWatcher and set its properties.
            // Params: Path, and filter
            using (var watcher =
                new FileSystemWatcher(@"C:\Users\sodonnell\Desktop\testinpactive", "*.pdf"))
            {

                watcher.InternalBufferSize = 8192000;
                // To watch SubDirectories 
                watcher.IncludeSubdirectories = true;

                FswHandler handler = new FswHandler();

                // Add event handlers.
                watcher.Created += handler.OnCreated;

                // Begin watching.
                watcher.EnableRaisingEvents = true;

                // Wait for the user to quit the program.
                Console.WriteLine("Press 'q' to quit the sample.");
                while (Console.Read() != 'q') ;
            }
        }

        public class FswHandler
        {
            // Specify what is done when a file is created
            public void OnCreated(object source, FileSystemEventArgs e)
            {
                // string output = @"C:\Users\sodonnell\Desktop\output";
                // Write out Path (Testing)
                //Console.WriteLine($"FILE: {e.FullPath} CHANGE-TYPE: {e.ChangeType}");
                
                var t = new Thread(new ThreadStart(() =>Methods.Optimize(e.FullPath, e.Name.Substring(7), Int32.Parse(e.FullPath.Substring(41, 6)))));
                Thread.Sleep(400);
                t.Start();
            }

        }
        public static void Main(string[] args)
        {
            string dir = @"C:\Users\sodonnell\Desktop\testinpactive\";
            var dirExists = Directory.Exists(dir);

            if (dirExists)
            {
                Console.WriteLine($"Directory Found: {dir}");
            }
            else
            {
                throw new Exception("ERROR: Failed to locate directory!");
            }

            Thread.Sleep(2000);

            Run();
        }

    }
}