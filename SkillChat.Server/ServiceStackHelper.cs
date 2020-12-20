using ServiceStack;

namespace SkillChat.Server
{
    public static class ServiceStackHelper
    {
        static ServiceStackHelper()
        {
            string licenseKeyText = @"TRIAL30WEB-e1JlZjpUUklBTDMwV0VCLE5hbWU6MTIvMjAvMjAyMCBhM2U1M2FiYjJmZWU0ODViOWYyMTg3YTZiZDM0MmMyNyxUeXBlOlRyaWFsLE1ldGE6MCxIYXNoOlFEUmFIR01tMTcrRExDWDRydlVZNTFuVWV2emx1MVZwVTl4U2FhKzJzdG9MUjF4NU5KbjJCM3Y3QitmTnZDUXR5NmFQNjduODdlTy9rc0dHaXpGdFVMaHRWdjJYeE5nblM0RS9RMmU1dUpHTzNkZFZITTNIWjRsVlBmYU96TnV5WjEvR1dCSTgvcDE3LytKbiszNGVEQy9zbHIzT2QvZzBjRUR5ZGRrcUNlbz0sRXhwaXJ5OjIwMjEtMDEtMTl9";
            Licensing.RegisterLicense(licenseKeyText);
        }

        public static void Help() { }
    }
}