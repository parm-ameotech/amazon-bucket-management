using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Core.Common.Infrastructure.Services.AmazonService
{
    public interface IAmazonService
    {
        void UploadDocument(byte[] pdfBytesToUpload, string pdfDocumentId, DocType documentType = DocType.PDF);

        string UploadImage(byte[] imageBytesToUpload, string contentType);

        byte[] DownloadPDF(string keyName);

        string GetPdfURL(string keyName);

        void DeleteDocument(string keyName);

        string GetFileURL(string keyName);

        void UploadHtmlDocument(byte[] htmlBytesToUpload, string htmlDocumentId);
    }
}
