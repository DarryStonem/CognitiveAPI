using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using Newtonsoft.Json;

namespace PagaloopCognitive.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CognitiveController : ControllerBase
    {
        string SUBSCRIPTION_KEY = "";
        string ENDPOINT = "";
        string ConnectionString = "";

        private readonly ILogger<CognitiveController> _logger;

        public CognitiveController(ILogger<CognitiveController> logger)
        {
            _logger = logger;
        }

        [HttpPost("{userid}/Compare")]
        public async Task<ActionResult> CompareImages(string userid, CompareRequest request)
        {
            IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);
            var response = new UploadResponse()
            {
                UserId = userid.ToString()
            };

            IList<Guid?> targetFaceIds = new List<Guid?>();

            targetFaceIds.Add(Guid.Parse(request.ComparasionFaceId));

            var result = await FindSimilar(client, Guid.Parse(request.BaseFaceId), targetFaceIds);
            var json = JsonConvert.SerializeObject(result.FirstOrDefault());
            response.Response = json;
            return Ok(response);
        }

        [HttpPost("{userid}/Upload")]
        [DisableRequestSizeLimit]
        public async Task<ActionResult> Upload(string userid, CognitiveModel request)
        {
            CloudStorageAccount storageConnection = CloudStorageAccount.Parse(ConnectionString);

            CloudBlobClient blobClient = storageConnection.CreateCloudBlobClient();

            CloudBlobContainer container = blobClient.GetContainerReference("faceimages");

            if (await container.CreateIfNotExistsAsync())
            {
                await container.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
            }

            var fileName = Guid.NewGuid().ToString() + ".jpg";

            CloudBlockBlob cloudBlockBlob = container.GetBlockBlobReference(string.Format("{0}/{1}", userid, fileName));

            cloudBlockBlob.Properties.ContentType = "image/jpg";

            await cloudBlockBlob.UploadFromByteArrayAsync(request.ImgData, 0, request.ImgData.Length);

            var response = new UploadResponse()
            {
                FileURL = cloudBlockBlob.Uri.AbsoluteUri,
                UserId = userid.ToString()
            };

            IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);
            var face = await DetectFaceExtract(client, cloudBlockBlob.Uri.AbsoluteUri, RecognitionModel.Recognition02);
            if (face != null)
            {
                response.FaceId = face.FaceId.ToString();
            }

            var json = JsonConvert.SerializeObject(face);
            response.Response = json;
            return Ok(response);
        }

        private static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }

        private async Task<IList<SimilarFace>> FindSimilar(IFaceClient client, Guid faceId, IList<Guid?> targetFaceIds)
        {
            // Find a similar face(s) in the list of IDs. Comapring only the first in list for testing purposes.
            var response = await client.Face.FindSimilarAsync(faceId, null, null, targetFaceIds);
            return response;
        }

        private async Task<DetectedFace> DetectFaceExtract(IFaceClient client, string url, string recognitionModel)
        {
            var face = await client.Face.DetectWithUrlAsync(url,
                returnFaceAttributes: new List<FaceAttributeType> { FaceAttributeType.Accessories, FaceAttributeType.Age,
                FaceAttributeType.Blur, FaceAttributeType.Emotion, FaceAttributeType.Exposure, FaceAttributeType.FacialHair,
                FaceAttributeType.Gender, FaceAttributeType.Glasses, FaceAttributeType.Hair, FaceAttributeType.HeadPose,
                FaceAttributeType.Makeup, FaceAttributeType.Noise, FaceAttributeType.Occlusion, FaceAttributeType.Smile },
                recognitionModel: recognitionModel);

            return face.FirstOrDefault();
        }
    }
}
