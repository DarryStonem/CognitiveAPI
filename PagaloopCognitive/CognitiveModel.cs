using System;
namespace PagaloopCognitive
{
    public class CognitiveModel
    {
        public byte[] ImgData { get; set; }
        public string UserId { get; set; }
    }

    public class UploadResponse
    {
        public string UserId { get; set; }
        public string FileURL { get; set; }
        public string Response { get; set; }
        public string FaceId { get; set; }
    }

    public class CompareRequest
    {
        public string BaseFaceId { get; set; }
        public string ComparasionFaceId { get; set; }
    }
}