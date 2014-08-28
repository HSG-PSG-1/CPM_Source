// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StandardPdfRenderer.cs" company="SemanticArchitecture">
//   http://www.SemanticArchitecture.net
// </copyright>
// <summary>
//   This class is responsible for rendering a html text string to a PDF document
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ReportManagement
{
    using System.IO;
    using System.Collections.Generic;

    /// <summary>
    /// This class is responsible for rendering a html text string to a PDF document using the html renderer of iTextSharp.
    /// </summary>
    public class StandardPdfRenderer
    {
        private HtmlViewRenderer htmlViewRenderer;

        public System.Web.Mvc.ActionResult BinaryPdfData(System.Web.Mvc.Controller ctrlr, string pageTitle, string viewName, object model)
        {
            this.htmlViewRenderer = new HtmlViewRenderer();
            
            // Render the view html to a string.
            string htmlText = htmlViewRenderer.RenderViewToString(ctrlr, viewName, model);

            // Let the html be rendered into a PDF document through iTextSharp.
            byte[] buffer = RenderW(htmlText, pageTitle);

            // Return the PDF as a binary stream to the client.
            return new BinaryContentResult(buffer, "application/pdf");
        }

        public byte[] RenderW(string htmlText, string pageTitle)
        {
            string rootAbsPath = System.Web.HttpContext.Current.Server.MapPath("~/");
            string fileAbsPath = System.IO.Path.Combine(rootAbsPath, pageTitle + ".html");
            string filePdfAbsPath = System.IO.Path.Combine(rootAbsPath, pageTitle + ".pdf");

            try
            {

                using (StreamWriter w = new StreamWriter(fileAbsPath, true))
                {
                    w.WriteLine(htmlText); // Write the text
                }

                string piDirectory = @"C:\Program Files\wkhtmltopdf";
                System.Diagnostics.ProcessStartInfo pi = new System.Diagnostics.ProcessStartInfo(Path.Combine(piDirectory, "wkhtmltopdf.exe"));
                pi.CreateNoWindow = true;
                pi.UseShellExecute = false;
                pi.WorkingDirectory = piDirectory;
                pi.Arguments = "\"" + fileAbsPath + "\" \"" + filePdfAbsPath + "\"";

                using (var process = System.Diagnostics.Process.Start(pi))
                {
                    process.WaitForExit(99999);
                    System.Diagnostics.Debug.WriteLine(process.ExitCode);
                }

                if (System.IO.File.Exists(filePdfAbsPath))
                    return File.ReadAllBytes(filePdfAbsPath);
                else
                    return new byte[] { };
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message + @"\n\n" + ex.Message);
                return new byte[] { };
            }
            finally 
            {//delete files
                File.Delete(fileAbsPath);
                File.Delete(filePdfAbsPath);
            }
        }
    }
}