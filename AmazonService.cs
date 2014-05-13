using System;
using Amazon.S3.Model;
using Amazon.S3;
using System.IO;
using System.Configuration;
using Core.Common.Infrastructure.Services.AmazonService;


namespace Core.Common.Infrastructure.Services.AmazonService
{
    public class AmazonService : IAmazonService
    {
        #region Private memebers

        private string ImagesBucketName { get { return ConfigurationManager.AppSettings["AWSPrestaProductImagesBucketName"]; } }

        private string DocsBucketName { get { return ConfigurationManager.AppSettings["AWSPrestaDocsBucketName"]; } }

        private string AWSAccessKey { get { return ConfigurationManager.AppSettings["AWSAccessKey"]; } }

        private string AWSSecretAccessKey { get { return ConfigurationManager.AppSettings["AWSSecretAccessKey"]; } }

        private string KeyName { get { return new Random().Next(1000).ToString(); } }

        private Amazon.S3.AmazonS3 GetClient
        {
            get
            {
                return Amazon.AWSClientFactory.CreateAmazonS3Client(AWSAccessKey, AWSSecretAccessKey);
            }
        }

        private byte[] ReadToEnd(Stream stream)
        {
            MemoryStream ms = new MemoryStream();
            byte[] buffer = new byte[1024];
            int bytes;
            while ((bytes = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, bytes);
            }
            byte[] output = ms.ToArray();
            return output;
        }

        #endregion

        /// <summary>
        /// To upload a agreement pdf to the svn amazon web service.
        /// </summary>
        /// <param name="pdfBytesToUpload">agreement pdf in byte array</param>
        /// <param name="pdfDocumentId">agreement id of the customer object</param>
        public void UploadDocument(byte[] pdfBytesToUpload, string pdfDocumentId, DocType documentType = DocType.PDF)
        {
            try
            {
                using (var client = GetClient)
                {
                    using (MemoryStream mStream = new MemoryStream(pdfBytesToUpload))
                    {
                        PutObjectRequest request = new PutObjectRequest();
                        request.WithBucketName(DocsBucketName)
                            .WithKey(pdfDocumentId);

                        if (documentType == DocType.PDF)
                            request.WithContentType("application/pdf");
                        else if (documentType == DocType.HTML)
                            request.WithContentType("text/html");
                        else
                            request.WithContentType("application/pdf");

                        request.WithInputStream(mStream);

                        S3Response response = client.PutObject(request);
                        response.Dispose();
                    }
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new Exception("Please check the provided AWS Credentials.");

                }
                else
                {
                    throw new Exception(string.Format("An error occurred with the message '{0}' when writing an object", amazonS3Exception.Message));
                }
            }
        }

        public void UploadHtmlDocument(byte[] htmlBytesToUpload, string htmlDocumentId)
        {
            UploadDocument(htmlBytesToUpload, htmlDocumentId, DocType.HTML);
        }

        /// <summary>
        /// To upload an image to amazon service bucket.
        /// </summary>
        /// <param name="imageBytesToUpload">image in byte array</param>
        /// <param name="contentType">content type of the image.</param>
        /// <returns>Url of the image from amazon web service</returns>
        public string UploadImage(byte[] imageBytesToUpload, string contentType)
        {
            try
            {
                using (var client = GetClient)
                {
                    string keyName = Guid.NewGuid().ToString();
                    using (MemoryStream mStream = new MemoryStream(imageBytesToUpload))
                    {
                        PutObjectRequest request = new PutObjectRequest();
                        request.WithBucketName(ImagesBucketName)
                            .WithKey(keyName);

                        request.WithContentType(contentType);

                        request.WithInputStream(mStream);

                        S3Response response = client.PutObject(request);

                        response.Dispose();
                    }

                    GetPreSignedUrlRequest requestUrl = new GetPreSignedUrlRequest()
                                                          .WithBucketName(ImagesBucketName)
                                                          .WithKey(keyName)
                                                          .WithProtocol(Protocol.HTTP)
                                                          .WithExpires(DateTime.Now.AddYears(10));

                    return client.GetPreSignedURL(requestUrl);
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new Exception("Please check the provided AWS Credentials.");

                }
                else
                {
                    throw new Exception(string.Format("An error occurred with the message '{0}' when writing an object", amazonS3Exception.Message));
                }
            }
        }

        public string GetFileURL(string keyName)
        {
            return GetPdfURL(keyName);
        }

        public string GetPdfURL(string keyName)
        {
            try
            {
                using (var client = GetClient)
                {

                    GetPreSignedUrlRequest requestUrl = new GetPreSignedUrlRequest()
                                                                  .WithBucketName(DocsBucketName)
                                                                  .WithKey(keyName)
                                                                  .WithProtocol(Protocol.HTTP)
                                                                  .WithExpires(DateTime.Now.AddYears(10));
                    return client.GetPreSignedURL(requestUrl);
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new Exception("Please check the provided AWS Credentials.");

                }
                else
                {
                    throw new Exception(string.Format("An error occurred with the message '{0}' when writing an object", amazonS3Exception.Message));
                }
            }
        }

        /// <summary>
        /// Download Pdf from amazon service bucket.
        /// </summary>
        /// <param name="keyName">The AgreementId of the customer object</param>
        /// <returns></returns>
        public byte[] DownloadPDF(string keyName)
        {
            byte[] pdfBytesToReturn = null;
            try
            {
                GetObjectRequest request = new GetObjectRequest().WithBucketName(DocsBucketName).WithKey(keyName);

                using (GetObjectResponse response = GetClient.GetObject(request))
                {
                    pdfBytesToReturn = ReadToEnd(response.ResponseStream);
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new Exception("Please check the provided AWS Credentials.");
                }
                else
                {
                    throw new Exception(string.Format("An error occurred with the message '{0}' when writing an object", amazonS3Exception.Message));
                }
            }
            return pdfBytesToReturn;
        }


        public void DeleteDocument(string keyName)
        {
            try
            {
                using (var client = GetClient)
                {

                    DeleteObjectRequest request = new DeleteObjectRequest();
                    request.WithBucketName(ImagesBucketName)
                        .WithKey(keyName);

                    client.DeleteObject(request);
                }
            }
            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null &&
                    (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") ||
                    amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    throw new Exception("Please check the provided AWS Credentials.");
                }
                else
                {
                    throw new Exception(string.Format("An error occurred with the message '{0}' when deleting an object", amazonS3Exception.Message));
                }
            }
        }
    }
}
