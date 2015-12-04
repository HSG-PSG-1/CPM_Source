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
        public static readonly char dirPathSep = System.IO.Path.DirectorySeparatorChar;
        
        public const char webPathSep = '/';
        
        public enum result
        { 
            successful,
            emptyNoFile,
            fileUploadIssue,
            contentLength,
            duplicate,
            noextension
        }

        public enum mode
        {
            header,
            detail,
            asyncHeader,
            asyncDetail
        }

        static string GetHD(mode upMode) {
            switch (upMode)
            {
                case mode.detail: return "D";
                case mode.header: return "H";
                case mode.asyncHeader: return "H_Temp";
                case mode.asyncDetail: return "D_Temp";
                default: return "H";//non-reachable
            }
        }

        #endregion

        #region Upload
        
        public static result UploadAndSave(HttpPostedFileBase upFile, ref string docName, string ClaimGUID, int? DetailId, mode upMode)
        {
            #region Init variables
            
            string subsubDir = GetHD(upMode); bool hasDetail = (DetailId != null);
            result resultIO = result.emptyNoFile;

            string ext = Path.GetExtension(upFile.FileName);//Get extension
            docName = Path.GetFileNameWithoutExtension(upFile.FileName);//Get only file name

            #endregion

            #region Issue with file name/ path / extension
            
            if (upFile == null || string.IsNullOrEmpty(upFile.FileName) || upFile.ContentLength < 1)
                return resultIO;
            else if(string.IsNullOrEmpty(ext))
                //Security (review in future)http://www.dreamincode.net/code/snippet1796.htm
                //if (ext.ToLower() == "exe" || ext.ToLower() == "ddl")
                return result.noextension;
            
            #endregion

            try
            {
                if (upFile.ContentLength > Config.MaxFileSizMB*1024*1024)
                    return result.contentLength;
                else
                {
                    //Get full path
                    string fullPath = CheckOrCreateDirectory(Config.UploadPath, ClaimGUID, subsubDir);
                    //Special case for Details dir (check & create a claimID/D/detailID directory)
                    if (hasDetail) fullPath = CheckOrCreateDirectory(fullPath, DetailId.ToString());
                    // Gen doc name
                    docName = docName + ext;
                    // Check file duplication
                    if (//upMode != mode.asyncDetail && upMode != mode.asyncHeader &&
                        File.Exists(Path.Combine(fullPath, docName)))
                        return result.duplicate;//Duplicate file exists!

                    // All OK - so finally upload
                    upFile.SaveAs(Path.Combine(fullPath, docName));//Save or Overwrite the file
                }
            }
            catch { return result.fileUploadIssue; }

            return result.successful;
        }

        #endregion

        #region Check / Create / Delete Directory & File

        public static string CheckOrCreateDirectory(string uploadPath, string dir, string subDir)
        {//i.e. ../../Files/2/H
            uploadPath = CheckOrCreateDirectory(uploadPath, dir);//Check and create directory
            return CheckOrCreateDirectory(uploadPath, subDir);//Check and create SUB directory
        }

        public static string CheckOrCreateDirectory(string uploadPath, string dirName)
        {
            if (!Directory.Exists(Path.Combine(uploadPath, dirName)))//Check and create directory
                Directory.CreateDirectory(Path.Combine(uploadPath, dirName));
            
            return Path.Combine(uploadPath, dirName);
        }
                
        public static void EmptyDirectory(string delPath)
        {
            if (!Directory.Exists(delPath) || delPath == Config.UploadPath)
                return; // avoid worst cases
            try
            {
                Directory.Delete(delPath, true);
            }
            catch (System.IO.IOException ex)
            {
                //Or refer the following to set system attributes when delete
                //http://stackoverflow.com/questions/611921/how-do-i-delete-a-directory-with-read-only-files-in-c
            }
        }

        public static bool DeleteFile(string FileName)
        {
            try
            {
                string FilePath = Path.Combine(Config.UploadPath, FileName);

                if (File.Exists(FilePath))
                    File.Delete(FilePath);
                
                return true; // HT: If file doesn't exist - we need not worry to delete it!
                
            }
            catch { return false; }

            //return false;
        }

        #endregion

        #region Claim File specific functions

        public static string GetClaimFilePath(string ClaimGUID, int? ClaimDetailID, mode upMode, string FileName, bool webURL)
        {
            if (string.IsNullOrEmpty(ClaimGUID) || (string.IsNullOrEmpty(FileName) && webURL))
                return "#"; 

            string basePath = webURL ? Config.DownloadUrl : Config.UploadPath;
            char sep = (webURL ? webPathSep : dirPathSep);
            string dirPath = basePath + sep + ClaimGUID + sep + GetHD(upMode); // might be web url or physical path so can't use Path.Combine
            
            if (ClaimDetailID != null) //Special case for Details file
                dirPath = dirPath + sep + ClaimDetailID.Value.ToString();

            return string.IsNullOrEmpty(FileName) ? dirPath : dirPath + sep + FileName;
        }

        public static string Merge(string uri1, string uri2)
        {
            uri1 = uri1.TrimEnd('/');
            uri2 = uri2.TrimStart('/');
            return string.Format("{0}/{1}", uri1, uri2);
        }

        public static string GetClaimDirPathForDelete(int ClaimID, int? ClaimDetailID, string ClaimGUID, bool IsAsync)
        {//Called from Claim-delete or ClaimDetail delete
           string claimPath = Path.Combine(Config.UploadPath, (IsAsync?ClaimGUID.ToString():ClaimID.ToString()));
           if (ClaimDetailID == null)
               return claimPath; // returned to Claim - delete
           else //if (DetailID != null) 
               return Path.Combine(Path.Combine(claimPath, GetHD(IsAsync ? mode.asyncDetail : mode.detail)), ClaimDetailID.Value.ToString()); //// returned to ClaimDetail (Item) - delete
        }

        public static string GetClaimFilesTempFolder(string ClaimGUID, bool IsHeader)
        {//Called from Claim-delete or ClaimDetail delete
            string claimPath = Path.Combine(Config.UploadPath, ClaimGUID.ToString());
            return Path.Combine(claimPath, GetHD(IsHeader ? mode.asyncHeader : mode.asyncDetail));
        }

        public static bool DeleteClaimFile(string docName, int ClaimID, int? ClaimDetailId, FileIO.mode upMode)
        { return DeleteClaimFile(docName, ClaimID.ToString(), ClaimDetailId, upMode); }
        public static bool DeleteClaimFile(string docName, string ClaimGUID, int? ClaimDetailId, FileIO.mode upMode)
        {
            return FileIO.DeleteFile(GetClaimFilePath(ClaimGUID, ClaimDetailId, upMode, docName, false));            
        }
        
        #endregion

        #region Move / Get File download code

        public static void MoveAsyncClaimFiles(int claimID,string ClaimGUID, int? oldClaimDetailID, int? claimDetailID, bool isHeader)
        {// Move all Async uploaded files from H_Temp to H
            
            mode FMode = (isHeader ? mode.header : mode.detail);
            mode aFMode = (isHeader ? mode.asyncHeader : mode.asyncDetail);

            string sourcePath = GetClaimFilePath(ClaimGUID, oldClaimDetailID, aFMode, "", false);
            string targetPath = GetClaimFilePath(claimID.ToString(), claimDetailID, FMode, "", false);

            if (!Directory.Exists(sourcePath))
                return;//Means there were only delete records which are already deleted

            //check if the target directory exists (special case for first time upload during Async mode)
            if (!Directory.Exists(targetPath))   
                Directory.CreateDirectory(targetPath);

            DirectoryInfo di = new DirectoryInfo(sourcePath);
            //MOVE all the files into the new directory
            foreach (FileInfo fi in di.GetFiles())
                fi.CopyTo(Path.Combine(targetPath, fi.Name), true);
            
            // !! HT - handled after Claim entry save 
            //Finally empty the source temp DIR
            //EmptyDirectory(sourcePath);
        }

        public static string getFileDownloadCode(string FileName, string ClaimGUID)
        {
            string sepr = CPM.DAL.FileHeader.sep;

            string codeStr = FileName + sepr + ClaimGUID + sepr + (false).ToString();
            codeStr = HttpUtility.UrlEncode(Crypto.EncodeStr(codeStr.ToString(), true));
            // Make sure you do UrlEncode TWICE in code to get the code!!!
            return codeStr;
        }

        public static string getFileDownloadActionCode(string FileName, int ClaimID, int? ClaimDetailID)
        {
            bool isDetailFile = ClaimDetailID.HasValue;
            string sepr = CPM.DAL.FileHeader.sep;
            
            System.Text.StringBuilder codeStr = new System.Text.StringBuilder(FileName + sepr + ClaimID.ToString() + sepr);
            if (isDetailFile) codeStr.Append(ClaimDetailID.ToString() + sepr);
            codeStr.Append((false).ToString());

            // Make sure you do UrlEncode TWICE in code to get the code!!!
            return (isDetailFile?"GetFileD?":"GetFile?") + HttpUtility.UrlEncode(Crypto.EncodeStr(codeStr.ToString(), true));
        }

        #endregion

        //Merge two directories
        //http://stackoverflow.com/questions/9053564/c-sharp-merge-one-directory-with-another
    }
}
