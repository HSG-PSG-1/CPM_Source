using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using System.IO;

namespace CPM.Helper
{
    public class FileIO
    {
        #region Variables

        public const char webPathSep = '/', fileNameSep = '~';
        public static string sep = ";", fileNameSearchPattern = "{0}" + fileNameSep + "*.*"; // must match GetFileName
        public static readonly char dirPathSep = System.IO.Path.DirectorySeparatorChar;
        
        public enum result
        { 
            successful,
            emptyNoFile,
            fileUploadIssue,
            contentLength,
            duplicate,
            noextension
        }

        static string GetHD(bool? isHdr = true)
        { 
            return (isHdr??true)?"H":"D";
        }

        #endregion

        #region Upload

        public static result UploadAndSave(HttpPostedFileBase upFile, int ClaimID, string ClaimGUID, int? DetailId)
        {
            #region Init variables

            string dir = (ClaimID > 0)?ClaimID.ToString():ClaimGUID; 
            string subDir = GetHD(!DetailId.HasValue);
            string fullPath = Config.UploadPath;
            bool hasDetail = (DetailId != null);
            result resultIO = result.emptyNoFile;
            string ext = Path.GetExtension(upFile.FileName);//Get extension
            string fileNm = Path.GetFileName(upFile.FileName);// upFile.FileName;//Get only file name

            #endregion

            #region Issue with file name/ path / extension
            
            if (upFile == null || string.IsNullOrEmpty(upFile.FileName) || upFile.ContentLength < 1)
                return resultIO;
            else if(string.IsNullOrEmpty(ext))
                //Security (review in future)http://www.dreamincode.net/code/snippet1796.htm
                //if (ext.ToLower() == "exe" || ext.ToLower() == "ddl")
                return result.noextension;
            
            if (upFile.ContentLength > Config.MaxFileSizMB*1024*1024)
                    return result.contentLength;
            
            #endregion

            try
            {
                fullPath = CheckAndCreateDirectory(Config.UploadPath, dir, subDir, (hasDetail ? DetailId.ToString() : ""));
                string oriFileNm = fileNm;
                // Gen doc name
                fileNm = GetFileName(fileNm, ClaimID, ClaimGUID, DetailId);
                // Check file duplication
                if (File.Exists(Path.Combine(fullPath, fileNm)) || File.Exists(Path.Combine(fullPath, oriFileNm))) // skip checkto allow the user to overwrite a new version
                    return result.duplicate;//Duplicate file exists!

                // All OK - so finally upload
                upFile.SaveAs(Path.Combine(fullPath, fileNm));//Save or Overwrite the file
                //reset original filename

            }
            catch { return result.fileUploadIssue; }

            return result.successful;
        }

        #endregion

        #region Check / Create / Delete Directory & File

        public static string CheckAndCreateDirectory(string uploadPath, params string[] directories)
        {
            foreach (string dir in directories)
            {
                if (string.IsNullOrEmpty(dir.Trim())) continue;
                uploadPath = Path.Combine(uploadPath, dir);
                 if(!Directory.Exists(uploadPath))//Check and create directory
                     Directory.CreateDirectory(uploadPath);
            }
            return uploadPath;
        }        

        /// <summary>
        /// Depth-first recursive delete, with handling for descendant 
        /// directories open in Windows Explorer.
        /// </summary>
        public static void DeleteDirectory(string path)
        {
            if (!Directory.Exists(path) || path == Config.UploadPath)
                return; // avoid worst cases

            string[] files = Directory.GetFiles(path);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string directory in Directory.GetDirectories(path))
                DeleteDirectory(directory);

            try { Directory.Delete(path, true); }
            catch (IOException) { Directory.Delete(path, true); }
            catch (UnauthorizedAccessException) { Directory.Delete(path, true); }
        }
        
        #endregion

        #region Claim File specific functions
        /*public static string Merge(string uri1, string uri2)
        {
            uri1 = uri1.TrimEnd('/');
            uri2 = uri2.TrimStart('/');
            return string.Format("{0}/{1}", uri1, uri2);
        }*/

        public static string MergePath(string basePath, bool webURL, params string[] paths)
        {
            string fullPath = basePath;
            foreach (string dir in paths)
            {
                if (dir.Trim().Length > 0)
                {
                    if (!webURL)
                        fullPath = Path.Combine(fullPath, dir);
                    else // is web url
                        fullPath = fullPath + dirPathSep + dir;
                }
            }
            return fullPath;
        }

        public static string GetFileName(string FileName, int ClaimID, string ClaimGUID = "", int? ClaimDetailID = null)
        {
            if ((FileName ?? "").Trim().Length == 0) return string.Empty;

            if (ClaimID > 0 && (ClaimDetailID??1) > 0) // if its a new claim then NO need to change file name beause at the end we'll directly change the GID folder to ID
                FileName = (ClaimGUID.Length > 0) ? (ClaimGUID + fileNameSep + FileName) : FileName; // GUID is sent only if its web and temp

            return FileName;
        }

        public static string GetClaimFilePath(int ClaimID, string ClaimGUID, string FileName, int? ClaimDetailID = null, bool webURL = false)
        {
            if (string.IsNullOrEmpty(FileName) && webURL)
                return "#";

            string basePath = GetClaimFilesDirectory(ClaimID, ClaimGUID, ClaimDetailID, webURL);
            return MergePath(basePath, webURL, GetFileName(FileName, ClaimID, ClaimGUID, ClaimDetailID));
        }

        public static string GetClaimFilesDirectory(int ClaimID, string ClaimGUID, int? ClaimDetailID = null, bool webURL = false)
        {
            string basePath = webURL ? Config.DownloadUrl : Config.UploadPath;
            string claimDir = (ClaimID > 0) ? ClaimID.ToString() : ClaimGUID;
            bool isHdr = !ClaimDetailID.HasValue;
            string detailDir = !isHdr ? ClaimDetailID.Value.ToString() : "";
            
            return MergePath(basePath, webURL,claimDir, GetHD(isHdr), detailDir);
        }
        
        public static bool DeleteClaimFile(int ClaimID, string ClaimGUID, string fileName, int? ClaimDetailID = null)
        {
            try
            {
                string FilePath = GetClaimFilePath(ClaimID, ClaimGUID, fileName, ClaimDetailID);

                if (File.Exists(FilePath))
                    File.Delete(FilePath);

                return true; // HT: If file doesn't exist - we need not worry to delete it!

            }
            catch { return false; }
        }
        
        #endregion

        #region Move / Cleanup / Get File download code

        public static void MoveFilesFolderNewClaimOrItem(int ClaimID, string ClaimGUID, int? OldClaimDetailID = null, int? ClaimDetailID = null)
        {
            // If its a new Claim - just need to rename GUID to NewID folder (same for new item folder) and rename ClaimDetailId
            //  For H (invoke only once)
            //  For D (invoke for each ClaimDetailId) : rename -1 to ClaimDetailID
            string sourcePath = Directory.GetParent(GetClaimFilesDirectory(0, ClaimGUID)).FullName;

            if (Directory.Exists(sourcePath)) // GUID directory exists means its a new claim
            {
                string targetPath = Directory.GetParent(GetClaimFilesDirectory(ClaimID, "")).FullName;
                new DirectoryInfo(sourcePath).MoveTo(targetPath);
                if (!ClaimDetailID.HasValue) return; // header so return
            }
            // Only for Detail files
            sourcePath = GetClaimFilesDirectory(ClaimID, "", OldClaimDetailID);
            if (OldClaimDetailID < 1) // ClaimID < 1 is never possible because we always set the new ClaimId in child objects
            {
                if (Directory.Exists(sourcePath))
                    new DirectoryInfo(sourcePath).MoveTo(GetClaimFilesDirectory(ClaimID, "", ClaimDetailID));
                return;
            }
        }

        public static void StripGUIDFromClaimFileName(int ClaimID, string ClaimGUID, int? OldClaimDetailID = null, int? ClaimDetailID = null)
        {
            // If its a new Claim use - MoveFilesFolderNewClaimOrItem
            //  For H : rename each GUID_fileH.ext to fileH.ext
            //  For D : rename each GUID_fileD.ext to fileD.ext            

            string sourcePath = GetClaimFilesDirectory(ClaimID, "", ClaimDetailID);
            // Only for existing H or D
            if (Directory.Exists(sourcePath))
            {
                foreach (FileInfo fi in GetFiles(sourcePath, ClaimGUID))
                    fi.MoveTo(Path.Combine(sourcePath, fi.Name.Replace(ClaimGUID + fileNameSep, "")));
            }
        }
        public static void CleanTempUpload(int ClaimID, string ClaimGUID)
        {
            string sourcePath = GetClaimFilesDirectory(ClaimID, ClaimGUID);

            if (ClaimID < 1) // new claim so delete GUID directory
            { DeleteDirectory(Directory.GetParent(sourcePath).FullName); return; }

            // Header temp cleanup
            if (Directory.Exists(sourcePath))
                foreach (FileInfo fi in GetFiles(sourcePath, ClaimGUID))
                    fi.Delete();
            // Detail temp cleanup
            sourcePath = Path.Combine(Directory.GetParent(sourcePath).FullName, GetHD(false));
            if (!Directory.Exists(sourcePath)) return; // No 'D'etail folder
            foreach (string delPath in Directory.GetDirectories(sourcePath))
            {
                int ClaimDetailID = 0;
                //same as dir - string delPath = Path.Combine(sourcePath, dir);
                if (Directory.Exists(delPath))
                {
                    string folderID = new DirectoryInfo(delPath).Name;
                    if (int.TryParse(folderID, out ClaimDetailID) && ClaimDetailID < 0)
                        DeleteDirectory(delPath); // delete temp directories like -1, -2
                    else
                        foreach (FileInfo fi in GetFiles(delPath, ClaimGUID))
                            fi.Delete();
                }
            }
        }

        public static FileInfo[] GetFiles(string sourcePath, string ClaimGUID)
        { 
            string searchPattern = String.Format(fileNameSearchPattern, ClaimGUID);
            if(Directory.Exists(sourcePath))
                return new DirectoryInfo(sourcePath).GetFiles(searchPattern);
            else
                return new FileInfo[]{}; // path NOT found so return empty
        }

        public static string getFileDownloadCode(string FileName, int ClaimID, string ClaimGUID)
        {
            string codeStr = FileName + sep + ClaimID.ToString() + sep + ClaimGUID.ToString();                
            codeStr = HttpUtility.UrlEncode(Crypto.EncodeStr(codeStr.ToString(), true));
            // Make sure you do UrlEncode TWICE in code to get the code!!!
            return codeStr;
        }

        public static string getFileDownloadActionCode(string FileName, int ClaimID, int? ClaimDetailID)
        {
            bool isDetailFile = ClaimDetailID.HasValue;
            
            System.Text.StringBuilder codeStr = new System.Text.StringBuilder(FileName + sep + ClaimID.ToString());
            if (isDetailFile) codeStr.Append(sep + ClaimDetailID.ToString());            

            // Make sure you do UrlEncode TWICE in code to get the code!!!
            return (isDetailFile?"GetFileD?":"GetFile?") + HttpUtility.UrlEncode(Crypto.EncodeStr(codeStr.ToString(), true));
        }

        #endregion        
    }
}
