using UnityEngine.Networking;

namespace API.API_Helper_Classes
{
    public class CustomCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // Always return true to bypass validation (not recommended for production)
            return true;
        }
    }
}