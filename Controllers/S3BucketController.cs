using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using S3TestAPI.Services;

namespace S3TestAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class S3BucketController : ControllerBase
    {
        private readonly IS3Service _service;

        public S3BucketController(IS3Service service) {
            _service = service;
        }

        [HttpGet]
        [Route("GetFiles/{bucketNmae}")]
        public async Task<IActionResult> GetFiles(string bucketNmae) {
            var response = await _service.FilesListAsync(bucketNmae);
            return Ok(response);
        }

        [HttpPost]
        [Route("CreateBucket/{bucketName}")]
        public async Task<IActionResult> CreateBucket([FromRoute] string bucketName) {
            var response = await _service.CreateBucketAsync(bucketName);
            return Ok(response);
        }

        [HttpPost]
        [Route("AddFile/{bucketName}")]
        public async Task<IActionResult> AddFile(IFormFile file, string bucketName) {
            var response = await _service.UploadFileAsync(file, bucketName);
            return Ok(response);
        }
    }
}