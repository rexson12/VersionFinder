using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace VersionFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            //if (args.Length == 0)
            //{
            //    Console.WriteLine("Please provide the path to the zip file as an argument.");
            //    return;
            //}

            string zipFilePath = "E:\\VersionFinder\\Testing\\Tour_Management_Asp.Net-main.zip";


            // Check if the file exists
            if (!File.Exists(zipFilePath))
            {
                Console.WriteLine($"File not found: {zipFilePath}");
                return;
            }

            // Extract the zip file to a temporary directory
            string extractPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(extractPath);

            try
            {
                ZipFile.ExtractToDirectory(zipFilePath, extractPath);

                // Find .csproj files in the extracted directory
                string[] projectFiles = Directory.GetFiles(extractPath, "*.csproj", SearchOption.AllDirectories);

                if (projectFiles.Length == 0)
                {
                    Console.WriteLine("No project file found (.csproj) in the extracted directory.");
                    return;
                }

                // Dictionary to store framework versions keyed by project file
                Dictionary<string, string> frameworkVersions = new Dictionary<string, string>();

                foreach (var projectFile in projectFiles)
                {
                    if (IsWebApplicationProject(projectFile))
                    {
                        string frameworkVersion = GetFrameworkVersionFromProjectFile(projectFile);

                        if (!string.IsNullOrEmpty(frameworkVersion))
                        {
                            frameworkVersions.Add(projectFile, frameworkVersion);
                        }
                    }
                }

                if (frameworkVersions.Count > 0)
                {
                    Console.WriteLine("Framework versions for web application projects:");
                    foreach (var kvp in frameworkVersions)
                    {
                        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                    }
                }
                else
                {
                    Console.WriteLine("No web application project found or unable to determine framework versions.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                // Clean up: delete the extracted directory
                Directory.Delete(extractPath, true);
            }
        }

        static bool IsWebApplicationProject(string projectFilePath)
        {
            try
            {
                // Load the project file as XML
                var doc = new XmlDocument();
                doc.Load(projectFilePath);

                // Define namespace manager to handle namespace in XML
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("msbuild", "http://schemas.microsoft.com/developer/msbuild/2003");

                // Check if the project file has WebApplication related elements
                var node = doc.SelectSingleNode("//msbuild:Project/msbuild:PropertyGroup/msbuild:OutputType", nsmgr);
                if (node != null && node.InnerText.Equals("Library", StringComparison.OrdinalIgnoreCase))
                {
                    // Check for additional web application identifiers
                    var webAppNode = doc.SelectSingleNode("//msbuild:Project/msbuild:PropertyGroup/msbuild:UseIISExpress", nsmgr);
                    if (webAppNode != null && webAppNode.InnerText.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    var projectTypeGuidsNode = doc.SelectSingleNode("//msbuild:Project/msbuild:PropertyGroup/msbuild:ProjectTypeGuids", nsmgr);
                    if (projectTypeGuidsNode != null && projectTypeGuidsNode.InnerText.Contains("{349c5851-65df-11da-9384-00065b846f21}"))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while checking project type for '{projectFilePath}': {ex.Message}");
            }
        }

        static string GetFrameworkVersionFromProjectFile(string projectFilePath)
        {
            try
            {
                // Load the project file as XML
                var doc = new XmlDocument();
                doc.Load(projectFilePath);

                // Define namespace manager to handle namespace in XML
                var nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("msbuild", "http://schemas.microsoft.com/developer/msbuild/2003");

                // Find the TargetFrameworkVersion or TargetFramework element
                var node = doc.SelectSingleNode("//msbuild:Project/msbuild:PropertyGroup/msbuild:TargetFrameworkVersion", nsmgr);
                if (node == null)
                {
                    node = doc.SelectSingleNode("//msbuild:Project/msbuild:PropertyGroup/msbuild:TargetFramework", nsmgr);
                }

                if (node != null)
                {
                    return node.InnerText;
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while reading project file '{projectFilePath}': {ex.Message}");
            }
        }
    }
}
