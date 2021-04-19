using Microsoft.AspNetCore.Http;
using S3TestAPI.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace S3TestAPI.Services
{
    public interface IS3Service
    {
        Task<S3Response> CreateBucketAsync(string bucketName);
        Task<S3Response> CreateFolderAsync(string foldername, string bucketName);
        Task<S3Response> DeleteFolderAsync(string bucketName, string foldername);
        Task<S3Response> DeleteFileFromFolderAsync(string bucketName, string foldername, string file);
        Task<S3Response> UploadFileToFolderAsync(IFormFile file, string bucketName, string folderName);
        Task<S3Response> UploadFileAsync(IFormFile file, string bucketName);
        Task<S3Response> GetFileFromFolderAsync(string bucketName, string folderName, string FileName, string target);
        Task<Stream> GetFileAsync(string key);
        Task<List<string>> FilesListAsync(string bucketName);
        Task<S3Response> OpenFileAsync(string bucketName, string folderName, string FileName);
        Task<S3Response> CopyingObjectAsync(string bucketName, string folderFile, string destinationfolder);
        Task<S3Response> DownloadFileAsync(string bucketName, string folderName, string FileName);

    }
}
